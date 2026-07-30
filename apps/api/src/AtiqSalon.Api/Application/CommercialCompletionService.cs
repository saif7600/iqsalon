using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class CommercialCompletionService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> ApplyDiscount(Guid approvalId, CancellationToken ct)
    {
        if (tenant.UserId is null) return CommercialResult.Fail("user", "Authenticated user is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var approval = await db.DiscountApprovalRequests.SingleOrDefaultAsync(x =>
            x.Id == approvalId && x.Status == "Approved" && x.AppliedAtUtc == null, ct);
        if (approval is null || !tenant.CanAccessBranch(approval.BranchId))
            return CommercialResult.Fail("approval", "Approved unapplied discount is unavailable.");
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == approval.SaleId && x.Status == "Draft", ct);
        if (sale is null || sale.PaidTotal > 0)
            return CommercialResult.Fail("sale", "Discounts can apply only to unpaid draft sales.");
        var lines = await db.SaleLines.Where(x => x.SaleId == sale.Id).OrderBy(x => x.Sequence).ToListAsync(ct);
        var discountable = lines.Sum(x => Math.Max(0, x.GrossAmount - x.DiscountAmount));
        if (discountable <= 0 || approval.RequestedAmount > discountable)
            return CommercialResult.Fail("amount", "Approved discount exceeds the remaining discountable amount.");

        var remaining = approval.RequestedAmount;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var available = Math.Max(0, line.GrossAmount - line.DiscountAmount);
            var share = index == lines.Count - 1
                ? remaining
                : CommercialRules.Round(approval.RequestedAmount * available / discountable);
            share = Math.Min(available, Math.Min(remaining, share));
            remaining = CommercialRules.Round(remaining - share);
            var calculation = CommercialRules.CalculateLine(line.Quantity, line.UnitPrice,
                line.DiscountAmount + share, line.TaxRateSnapshot, line.TaxInclusiveSnapshot);
            line.DiscountAmount = calculation.Discount;
            line.NetAmount = calculation.Net;
            line.TaxableAmount = calculation.Taxable;
            line.TaxAmount = calculation.Tax;
            line.LineTotal = calculation.Total;
        }
        sale.DiscountTotal = CommercialRules.Round(lines.Sum(x => x.DiscountAmount));
        sale.TaxableTotal = CommercialRules.Round(lines.Sum(x => x.TaxableAmount));
        sale.TaxTotal = CommercialRules.Round(lines.Sum(x => x.TaxAmount));
        sale.GrandTotal = CommercialRules.Round(lines.Sum(x => x.LineTotal) + sale.TipTotal);
        sale.BalanceDue = sale.GrandTotal;
        approval.AppliedByUserId = tenant.UserId;
        approval.AppliedAtUtc = DateTimeOffset.UtcNow;
        Audit(sale.OrganizationId, "discount.applied", approval.Id, "DiscountApprovalRequest");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(sale.Id, sale.SaleNumber);
    }

    public async Task<CommercialResult> RecordCashMovement(Guid tillId, CashMovementRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || request.Amount <= 0
            || request.Type is not ("CashIn" or "CashOut") || string.IsNullOrWhiteSpace(request.Reason))
            return CommercialResult.Fail("validation", "Type, positive amount, and reason are required.");
        var requiredPermission = request.Type == "CashOut" ? "tills.cash_out" : "tills.cash_in";
        if (!tenant.HasPermission(requiredPermission))
            return CommercialResult.Fail("permission", $"{requiredPermission} permission is required.");
        var till = await db.TillSessions.SingleOrDefaultAsync(x => x.Id == tillId && x.Status == "Open", ct);
        if (till is null || !tenant.CanAccessBranch(till.BranchId))
            return CommercialResult.Fail("till", "Open accessible till is required.");
        var movement = new CashMovement
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = till.OrganizationId,
            BranchId = till.BranchId,
            TillSessionId = till.Id,
            Type = request.Type,
            Amount = CommercialRules.Round(request.Amount),
            Reason = request.Reason.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        db.CashMovements.Add(movement);
        Audit(till.OrganizationId, $"till.{request.Type.ToLowerInvariant()}", movement.Id, "CashMovement");
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(movement.Id, movement.Id.ToString());
    }

    public async Task<CommercialResult> ApproveTillVariance(Guid tillId,
        ApproveTillVarianceRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null || string.IsNullOrWhiteSpace(request.Note))
            return CommercialResult.Fail("validation", "Approval note is required.");
        var till = await db.TillSessions.SingleOrDefaultAsync(x => x.Id == tillId && x.Status == "Closed", ct);
        if (till is null || !tenant.CanAccessBranch(till.BranchId) || till.Variance is null or 0)
            return CommercialResult.Fail("till", "A closed till with non-zero variance is required.");
        if (till.ClosedByUserId == tenant.UserId)
            return CommercialResult.Fail("segregation", "The till closer cannot approve their own variance.");
        if (till.VarianceApprovedAtUtc is not null)
            return CommercialResult.Success(till.Id, till.Id.ToString(), true);
        till.VarianceApprovedByUserId = tenant.UserId;
        till.VarianceApprovedAtUtc = DateTimeOffset.UtcNow;
        till.VarianceApprovalNote = request.Note.Trim();
        Audit(till.OrganizationId, "till.variance_approved", till.Id, "TillSession");
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(till.Id, till.Id.ToString());
    }

    private void Audit(Guid organizationId, string action, Guid entityId, string entityType) =>
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId,
            ActorUserId = tenant.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Source = "api",
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
}

public sealed record CashMovementRequest(string Type, decimal Amount, string Reason);
public sealed record ApproveTillVarianceRequest(string Note);
