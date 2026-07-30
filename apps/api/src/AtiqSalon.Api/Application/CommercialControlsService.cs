using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class CommercialControlsService(AppDbContext db, TenantContext tenant, CommissionService commissions)
{
    public async Task<CommercialResult> RefundSale(Guid saleId, RefundSaleRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || request.Amount <= 0
            || string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.Reason))
            return CommercialResult.Fail("validation", "Amount, reason, and idempotency key are required.");
        var replay = await db.Refunds.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == saleId, ct);
        if (sale is null || sale.Status != "Posted" || !tenant.CanAccessBranch(sale.BranchId))
            return CommercialResult.Fail("sale", "Only an accessible posted sale can be refunded.");
        var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.SaleId == sale.Id && x.Status == "Issued", ct);
        var method = await db.PaymentMethods.SingleOrDefaultAsync(x => x.Id == request.PaymentMethodId
            && x.OrganizationId == sale.OrganizationId && x.IsActive && x.SupportsRefund, ct);
        if (invoice is null || method is null) return CommercialResult.Fail("refund", "Invoice or refund method is unavailable.");
        var refunded = await db.Refunds.Where(x => x.SaleId == sale.Id).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        if (request.Amount > CommercialControlRules.AvailableRefund(sale.PaidTotal, refunded))
            return CommercialResult.Fail("amount", "Refund exceeds the remaining paid amount.");

        var settings = await db.OrganizationCommercialSettings.SingleAsync(x => x.OrganizationId == sale.OrganizationId, ct);
        var credit = new CreditNote
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            InvoiceId = invoice.Id,
            CreditNoteNumber = $"{settings.CreditNotePrefix}-{DateTimeOffset.UtcNow.Year}-{settings.NextCreditNoteSequence++:000000}",
            CurrencyCode = sale.CurrencyCode,
            Subtotal = request.Amount,
            GrandTotal = request.Amount,
            Reason = request.Reason.Trim(),
            IssuedByUserId = tenant.UserId.Value
        };
        var payment = new Payment
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            PaymentNumber = $"PAY-{DateTimeOffset.UtcNow.Year}-{settings.NextPaymentSequence++:000000}",
            CustomerId = sale.CustomerId,
            PaymentMethodId = method.Id,
            Direction = "Outbound",
            CurrencyCode = sale.CurrencyCode,
            Amount = request.Amount,
            Reference = request.Reference,
            IdempotencyKey = $"refund:{request.IdempotencyKey}",
            ReceivedByUserId = tenant.UserId,
            TillSessionId = request.TillSessionId
        };
        var refund = new Refund
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            CreditNoteId = credit.Id,
            PaymentId = payment.Id,
            Amount = request.Amount,
            Reason = request.Reason.Trim(),
            IdempotencyKey = request.IdempotencyKey,
            RefundedByUserId = tenant.UserId.Value
        };
        db.AddRange(credit, payment, refund);
        await commissions.ReverseForRefund(sale, refund, ct);
        Audit(sale.OrganizationId, "refund.completed", refund.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(refund.Id, credit.CreditNoteNumber);
    }

    public async Task<CommercialResult> CloseTill(Guid tillId, CloseTillRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || request.CountedCash < 0)
            return CommercialResult.Fail("validation", "A non-negative cash count is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var till = await db.TillSessions.SingleOrDefaultAsync(x => x.Id == tillId && x.Status == "Open", ct);
        if (till is null || !tenant.CanAccessBranch(till.BranchId))
            return CommercialResult.Fail("till", "Open till session was not found.");
        var cashPayments = await db.Payments.Where(x => x.TillSessionId == till.Id && x.Status == "Completed")
            .Join(db.PaymentMethods.Where(x => x.Type == "Cash"), x => x.PaymentMethodId, x => x.Id,
                (payment, _) => payment.Direction == "Outbound" ? -payment.Amount : payment.Amount).SumAsync(ct);
        var movements = await db.CashMovements.Where(x => x.TillSessionId == till.Id)
            .SumAsync(x => x.Type == "CashOut" ? -x.Amount : x.Amount, ct);
        till.ExpectedCash = CommercialRules.Round(till.OpeningFloat + cashPayments + movements);
        till.CountedCash = CommercialRules.Round(request.CountedCash);
        till.Variance = CommercialControlRules.TillVariance(till.ExpectedCash, till.CountedCash.Value);
        till.Status = "Closed";
        till.ClosedByUserId = tenant.UserId;
        till.ClosedAtUtc = DateTimeOffset.UtcNow;
        Audit(till.OrganizationId, "till.closed", till.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(till.Id, till.Id.ToString());
    }

    public async Task<CommercialResult> RequestDiscount(Guid saleId, RequestDiscountApproval request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Reason))
            return CommercialResult.Fail("validation", "A positive amount and reason are required.");
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == saleId && x.Status == "Draft", ct);
        if (sale is null || !tenant.CanAccessBranch(sale.BranchId))
            return CommercialResult.Fail("sale", "Only an accessible draft sale can request a discount.");
        var approval = new DiscountApprovalRequest
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            RequestedAmount = CommercialRules.Round(request.Amount),
            RequestedPercentage = sale.Subtotal == 0 ? 0 : CommercialRules.Round(request.Amount / sale.Subtotal * 100),
            Reason = request.Reason.Trim(),
            RequestedByUserId = tenant.UserId.Value
        };
        db.DiscountApprovalRequests.Add(approval);
        Audit(sale.OrganizationId, "discount.requested", approval.Id);
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(approval.Id, approval.Id.ToString());
    }

    public async Task<CommercialResult> DecideDiscount(Guid approvalId, DecideDiscountApproval request, CancellationToken ct)
    {
        if (tenant.UserId is null || request.Decision is not ("Approved" or "Rejected"))
            return CommercialResult.Fail("validation", "Decision must be Approved or Rejected.");
        var approval = await db.DiscountApprovalRequests.SingleOrDefaultAsync(x => x.Id == approvalId && x.Status == "Pending", ct);
        if (approval is null || !tenant.CanAccessBranch(approval.BranchId))
            return CommercialResult.Fail("approval", "Pending approval was not found.");
        if (approval.RequestedByUserId == tenant.UserId)
            return CommercialResult.Fail("segregation", "A requester cannot approve their own discount.");
        approval.Status = request.Decision;
        approval.DecisionNote = request.Note?.Trim();
        approval.DecidedByUserId = tenant.UserId;
        approval.DecidedAtUtc = DateTimeOffset.UtcNow;
        Audit(approval.OrganizationId, $"discount.{request.Decision.ToLowerInvariant()}", approval.Id);
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(approval.Id, approval.Id.ToString());
    }

    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = action.Split('.')[0],
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public static class CommercialControlRules
{
    public static decimal AvailableRefund(decimal paid, decimal refunded) =>
        CommercialRules.Round(Math.Max(0, paid - refunded));
    public static decimal TillVariance(decimal expected, decimal counted) =>
        CommercialRules.Round(counted - expected);
}

public sealed record RefundSaleRequest(Guid PaymentMethodId, decimal Amount, string Reason,
    string IdempotencyKey, string? Reference = null, Guid? TillSessionId = null);
public sealed record CloseTillRequest(decimal CountedCash);
public sealed record RequestDiscountApproval(decimal Amount, string Reason);
public sealed record DecideDiscountApproval(string Decision, string? Note);
