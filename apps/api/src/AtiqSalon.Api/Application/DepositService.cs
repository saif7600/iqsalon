using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class DepositService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> Create(CreateDepositRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "Branch access, positive amount, and idempotency key are required.");

        var replay = await db.Payments.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null)
        {
            var existing = await db.CustomerDeposits.SingleOrDefaultAsync(x => x.PaymentId == replay.Id, ct);
            return existing is null
                ? CommercialResult.Fail("idempotency", "The idempotency key belongs to another payment.")
                : CommercialResult.Success(existing.Id, existing.DepositNumber, true);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == request.BranchId
            && x.OrganizationId == request.OrganizationId && x.IsActive, ct);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId
            && x.OrganizationId == request.OrganizationId, ct);
        var method = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == request.PaymentMethodId
            && x.OrganizationId == request.OrganizationId && x.IsActive, ct);
        if (branch is null || customer is null || method is null)
            return CommercialResult.Fail("scope", "Branch, customer, or payment method is unavailable.");
        if (method.RequiresReference && string.IsNullOrWhiteSpace(request.Reference))
            return CommercialResult.Fail("reference", "This payment method requires a reference.");
        if (method.RequiresTillSession && request.TillSessionId is null)
            return CommercialResult.Fail("till", "This payment method requires an open till.");
        if (request.TillSessionId is { } tillId && !await db.TillSessions.AnyAsync(x =>
                x.Id == tillId && x.BranchId == request.BranchId && x.Status == "Open", ct))
            return CommercialResult.Fail("till", "Till session is unavailable.");

        var settings = await Settings(request.OrganizationId, ct);
        var payment = new Payment
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            PaymentNumber = $"PAY-{DateTimeOffset.UtcNow.Year}-{settings.NextPaymentSequence++:000000}",
            CustomerId = request.CustomerId,
            PaymentMethodId = method.Id,
            Amount = CommercialRules.Round(request.Amount),
            CurrencyCode = settings.DefaultCurrencyCode,
            Reference = request.Reference?.Trim(),
            IdempotencyKey = request.IdempotencyKey,
            ReceivedByUserId = tenant.UserId,
            TillSessionId = request.TillSessionId
        };
        var deposit = new CustomerDeposit
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            PaymentId = payment.Id,
            DepositNumber = $"DEP-{DateTimeOffset.UtcNow.Year}-{settings.NextDepositSequence++:000000}",
            CurrencyCode = settings.DefaultCurrencyCode,
            OriginalAmount = payment.Amount,
            AvailableAmount = payment.Amount
        };
        db.AddRange(payment, deposit, new PaymentAllocation
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            PaymentId = payment.Id,
            DepositId = deposit.Id,
            Amount = payment.Amount
        });
        Audit(request.OrganizationId, "deposit.created", deposit.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(deposit.Id, deposit.DepositNumber);
    }

    public async Task<CommercialResult> Apply(Guid depositId, ApplyDepositRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null || request.Amount <= 0)
            return CommercialResult.Fail("validation", "A positive amount is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var deposit = await db.CustomerDeposits.SingleOrDefaultAsync(x => x.Id == depositId && x.Status == "Available", ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId
            && x.Status != "Posted" && x.Status != "Voided", ct);
        if (deposit is null || sale is null || deposit.CustomerId != sale.CustomerId
            || deposit.OrganizationId != sale.OrganizationId || !tenant.CanAccessBranch(sale.BranchId))
            return CommercialResult.Fail("scope", "Deposit and sale must belong to the same customer and organization.");
        if (await db.DepositApplications.AnyAsync(x => x.DepositId == depositId
            && x.SaleId == request.SaleId && x.Id == request.ApplicationId, ct))
            return CommercialResult.Success(deposit.Id, deposit.DepositNumber, true);

        var applied = CommercialRules.Round(Math.Min(request.Amount,
            Math.Min(deposit.AvailableAmount, sale.BalanceDue)));
        if (applied <= 0) return CommercialResult.Fail("balance", "No deposit balance can be applied.");
        db.DepositApplications.Add(new DepositApplication
        {
            Id = request.ApplicationId,
            TenantId = deposit.TenantId,
            OrganizationId = deposit.OrganizationId,
            DepositId = deposit.Id,
            SaleId = sale.Id,
            Amount = applied,
            AppliedByUserId = tenant.UserId.Value
        });
        deposit.AvailableAmount = CommercialRules.Round(deposit.AvailableAmount - applied);
        deposit.Status = deposit.AvailableAmount == 0 ? "Consumed" : "Available";
        sale.PaidTotal = CommercialRules.Round(sale.PaidTotal + applied);
        sale.BalanceDue = CommercialRules.Round(Math.Max(0, sale.GrandTotal - sale.PaidTotal));
        sale.Status = sale.BalanceDue == 0 ? "Paid" : "PartiallyPaid";
        Audit(sale.OrganizationId, "deposit.applied", deposit.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(deposit.Id, deposit.DepositNumber);
    }

    private async Task<OrganizationCommercialSettings> Settings(Guid organizationId, CancellationToken ct)
    {
        var settings = await db.OrganizationCommercialSettings.SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct);
        if (settings is not null) return settings;
        settings = new OrganizationCommercialSettings
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId
        };
        db.OrganizationCommercialSettings.Add(settings);
        return settings;
    }

    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "Deposit",
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public sealed record CreateDepositRequest(Guid OrganizationId, Guid BranchId, Guid CustomerId,
    Guid PaymentMethodId, decimal Amount, string IdempotencyKey, string? Reference = null,
    Guid? TillSessionId = null);
public sealed record ApplyDepositRequest(Guid SaleId, decimal Amount, Guid ApplicationId);
