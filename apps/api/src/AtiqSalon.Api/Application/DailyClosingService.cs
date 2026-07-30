using System.Data;
using System.Text.Json;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class DailyClosingService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> Create(CreateDailyClosingRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
            return CommercialResult.Fail("scope", "Branch access is required.");
        var existing = await db.BranchDailyClosings.SingleOrDefaultAsync(x =>
            x.BranchId == request.BranchId && x.BusinessDate == request.BusinessDate, ct);
        if (existing is not null) return CommercialResult.Success(existing.Id, request.BusinessDate.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == request.BranchId
            && x.OrganizationId == request.OrganizationId && x.IsActive, ct);
        if (branch is null) return CommercialResult.Fail("branch", "Branch is unavailable.");
        if (await db.TillSessions.AnyAsync(x => x.BranchId == branch.Id && x.Status == "Open", ct))
            return CommercialResult.Fail("tills", "Every till must be closed before daily closing.");
        var (fromUtc, toUtc) = DailyClosingRules.UtcRange(request.BusinessDate, branch.TimeZone);
        var sales = await db.Sales.Where(x => x.BranchId == branch.Id
            && x.BusinessDate == request.BusinessDate && x.Status == "Posted").ToListAsync(ct);
        var saleIds = sales.Select(x => x.Id).ToArray();
        var invoices = await db.Invoices.Where(x => saleIds.Contains(x.SaleId)
            && x.Status == "Issued").ToListAsync(ct);
        var payments = await db.Payments.Where(x => x.BranchId == branch.Id && x.Status == "Completed"
            && x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc).ToListAsync(ct);
        var refunds = await db.Refunds.Where(x => x.BranchId == branch.Id
            && x.RefundedAtUtc >= fromUtc && x.RefundedAtUtc < toUtc).ToListAsync(ct);
        var tills = await db.TillSessions.Where(x => x.BranchId == branch.Id
            && x.ClosedAtUtc >= fromUtc && x.ClosedAtUtc < toUtc && x.Status == "Closed").ToListAsync(ct);
        var vat = await db.SaleLines.Where(x => saleIds.Contains(x.SaleId))
            .GroupBy(x => new { x.TaxCodeSnapshot, x.TaxRateSnapshot })
            .Select(x => new
            {
                Code = x.Key.TaxCodeSnapshot,
                Rate = x.Key.TaxRateSnapshot,
                Taxable = x.Sum(y => y.TaxableAmount),
                Tax = x.Sum(y => y.TaxAmount)
            }).ToListAsync(ct);
        var methodIds = payments.Select(x => x.PaymentMethodId).Distinct().ToArray();
        var methods = await db.PaymentMethods.Where(x => methodIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var paymentSummary = payments.GroupBy(x => new
        {
            x.PaymentMethodId,
            Name = methods.GetValueOrDefault(x.PaymentMethodId)?.Name ?? "Unknown",
            Type = methods.GetValueOrDefault(x.PaymentMethodId)?.Type ?? "Other"
        }).Select(x => new
        {
            x.Key.PaymentMethodId,
            x.Key.Name,
            x.Key.Type,
            Inbound = x.Where(y => y.Direction == "Inbound").Sum(y => y.Amount),
            Outbound = x.Where(y => y.Direction == "Outbound").Sum(y => y.Amount)
        });
        var settings = await db.OrganizationCommercialSettings.SingleAsync(x =>
            x.OrganizationId == request.OrganizationId, ct);
        var closing = new BranchDailyClosing
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = branch.Id,
            BusinessDate = request.BusinessDate,
            CurrencyCode = settings.DefaultCurrencyCode,
            GrossSales = CommercialRules.Round(sales.Sum(x => x.Subtotal)),
            Discounts = CommercialRules.Round(sales.Sum(x => x.DiscountTotal)),
            NetSales = CommercialRules.Round(sales.Sum(x => x.GrandTotal)),
            TaxTotal = CommercialRules.Round(sales.Sum(x => x.TaxTotal)),
            Tips = CommercialRules.Round(sales.Sum(x => x.TipTotal)),
            PaymentsIn = CommercialRules.Round(payments.Where(x => x.Direction == "Inbound").Sum(x => x.Amount)),
            RefundsOut = CommercialRules.Round(refunds.Sum(x => x.Amount)),
            ExpectedCash = CommercialRules.Round(tills.Sum(x => x.ExpectedCash)),
            CountedCash = CommercialRules.Round(tills.Sum(x => x.CountedCash ?? 0)),
            CashVariance = CommercialRules.Round(tills.Sum(x => x.Variance ?? 0)),
            PostedSaleCount = sales.Count,
            InvoiceCount = invoices.Count,
            RefundCount = refunds.Count,
            VatSummaryJson = JsonSerializer.Serialize(vat),
            PaymentSummaryJson = JsonSerializer.Serialize(paymentSummary),
            TillSummaryJson = JsonSerializer.Serialize(tills.Select(x => new
            {
                x.Id,
                x.OpenedByUserId,
                x.ClosedByUserId,
                x.OpeningFloat,
                x.ExpectedCash,
                x.CountedCash,
                x.Variance,
                x.OpenedAtUtc,
                x.ClosedAtUtc
            })),
            CreatedByUserId = tenant.UserId.Value
        };
        db.BranchDailyClosings.Add(closing);
        Audit(closing.OrganizationId, "daily_closing.created", closing.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(closing.Id, closing.BusinessDate.ToString());
    }

    public async Task<CommercialResult> Approve(Guid id, ApproveDailyClosingRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null) return CommercialResult.Fail("user", "Authenticated user is required.");
        var closing = await db.BranchDailyClosings.SingleOrDefaultAsync(x =>
            x.Id == id && x.Status == "PendingApproval", ct);
        if (closing is null || !tenant.CanAccessBranch(closing.BranchId))
            return CommercialResult.Fail("closing", "Pending closing is unavailable.");
        if (closing.CreatedByUserId == tenant.UserId)
            return CommercialResult.Fail("segregation", "The closing creator cannot approve their own closing.");
        closing.Status = "Approved";
        closing.ApprovedByUserId = tenant.UserId;
        closing.ApprovalNote = request.Note?.Trim();
        closing.ApprovedAtUtc = DateTimeOffset.UtcNow;
        Audit(closing.OrganizationId, "daily_closing.approved", closing.Id);
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(closing.Id, closing.BusinessDate.ToString());
    }

    private void Audit(Guid organizationId, string action, Guid entityId) =>
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId,
            ActorUserId = tenant.UserId,
            Action = action,
            EntityType = "BranchDailyClosing",
            EntityId = entityId.ToString(),
            Source = "api",
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
}

public static class DailyClosingRules
{
    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) UtcRange(DateOnly date, string timeZone)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, zone), TimeZoneInfo.ConvertTimeToUtc(localEnd, zone));
    }
}

public sealed record CreateDailyClosingRequest(Guid OrganizationId, Guid BranchId, DateOnly BusinessDate);
public sealed record ApproveDailyClosingRequest(string? Note);
