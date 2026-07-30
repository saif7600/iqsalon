using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class CommercialReportEndpoints
{
    public static IEndpointRouteBuilder MapCommercialReportApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/reports/commercial", async (Guid branchId, DateOnly from, DateOnly to,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (!tenant.CanAccessBranch(branchId)) return Results.Forbid();
            if (to < from || to.DayNumber - from.DayNumber > 366)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["period"] = ["The period must be ordered and no longer than 366 days."] });
            var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == branchId && x.IsActive, ct);
            if (branch is null) return Results.NotFound();
            var (fromUtc, _) = DailyClosingRules.UtcRange(from, branch.TimeZone);
            var (_, toUtc) = DailyClosingRules.UtcRange(to, branch.TimeZone);
            var sales = db.Sales.Where(x => x.BranchId == branchId && x.Status == "Posted"
                && x.BusinessDate >= from && x.BusinessDate <= to);
            var saleIds = await sales.Select(x => x.Id).ToArrayAsync(ct);
            var payments = db.Payments.Where(x => x.BranchId == branchId && x.Status == "Completed"
                && x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc);
            var paymentRows = await payments.ToListAsync(ct);
            var methodIds = paymentRows.Select(x => x.PaymentMethodId).Distinct().ToArray();
            var methods = await db.PaymentMethods.Where(x => methodIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            var paymentSummary = paymentRows.GroupBy(x => new
            {
                x.PaymentMethodId,
                Name = methods.GetValueOrDefault(x.PaymentMethodId)?.Name ?? "Unknown",
                Type = methods.GetValueOrDefault(x.PaymentMethodId)?.Type ?? "Other"
            }).Select(x => new
            {
                x.Key.PaymentMethodId,
                x.Key.Name,
                x.Key.Type,
                Inbound = CommercialRules.Round(x.Where(y => y.Direction == "Inbound").Sum(y => y.Amount)),
                Outbound = CommercialRules.Round(x.Where(y => y.Direction == "Outbound").Sum(y => y.Amount))
            }).OrderBy(x => x.Name).ToArray();
            var vat = await db.SaleLines.Where(x => saleIds.Contains(x.SaleId))
                .GroupBy(x => new { x.TaxCodeSnapshot, x.TaxRateSnapshot })
                .Select(x => new
                {
                    Code = x.Key.TaxCodeSnapshot,
                    Rate = x.Key.TaxRateSnapshot,
                    Taxable = x.Sum(y => y.TaxableAmount),
                    Tax = x.Sum(y => y.TaxAmount)
                }).OrderBy(x => x.Code).ToListAsync(ct);
            var salesSummary = await sales.GroupBy(_ => 1).Select(x => new
            {
                Count = x.Count(),
                Gross = x.Sum(y => y.Subtotal),
                Discounts = x.Sum(y => y.DiscountTotal),
                Tax = x.Sum(y => y.TaxTotal),
                Tips = x.Sum(y => y.TipTotal),
                Net = x.Sum(y => y.GrandTotal)
            }).SingleOrDefaultAsync(ct);
            var refunds = await db.Refunds.Where(x => x.BranchId == branchId
                && x.RefundedAtUtc >= fromUtc && x.RefundedAtUtc < toUtc)
                .GroupBy(_ => 1).Select(x => new { Count = x.Count(), Amount = x.Sum(y => y.Amount) })
                .SingleOrDefaultAsync(ct);
            var commissions = await db.CommissionLedgerEntries.Where(x => x.BranchId == branchId
                && x.BusinessDate >= from && x.BusinessDate <= to)
                .GroupBy(_ => 1).Select(x => new
                {
                    Earned = x.Where(y => y.EntryType == "Earned").Sum(y => y.Amount),
                    Reversed = -x.Where(y => y.EntryType == "Reversal").Sum(y => y.Amount),
                    Net = x.Sum(y => y.Amount)
                }).SingleOrDefaultAsync(ct);
            var giftCardIds = await db.GiftCards.Where(x => x.BranchId == branchId).Select(x => x.Id).ToArrayAsync(ct);
            var giftCardLiability = await db.GiftCardLedgerEntries.Where(x => giftCardIds.Contains(x.GiftCardId))
                .SumAsync(x => x.EntryType == "Redeem" ? -x.Amount : x.Amount, ct);
            var closingRows = await db.BranchDailyClosings.Where(x => x.BranchId == branchId
                && x.BusinessDate >= from && x.BusinessDate <= to)
                .OrderByDescending(x => x.BusinessDate).ToListAsync(ct);
            return Results.Ok(new
            {
                branch = new { branch.Id, branch.Name, branch.TimeZone },
                period = new { from, to },
                sales = salesSummary ?? new { Count = 0, Gross = 0m, Discounts = 0m, Tax = 0m, Tips = 0m, Net = 0m },
                refunds = refunds ?? new { Count = 0, Amount = 0m },
                payments = paymentSummary,
                vat,
                commissions = commissions ?? new { Earned = 0m, Reversed = 0m, Net = 0m },
                liabilities = new
                {
                    CustomerDeposits = await db.CustomerDeposits.Where(x => x.BranchId == branchId)
                        .SumAsync(x => x.AvailableAmount, ct),
                    GiftCards = CommercialRules.Round(giftCardLiability),
                    ActivePackages = await db.CustomerPackages.CountAsync(x => x.BranchId == branchId
                        && x.Status == "Active" && x.ExpiresAtUtc > DateTimeOffset.UtcNow, ct),
                    ActiveMemberships = await db.CustomerMemberships.CountAsync(x => x.BranchId == branchId
                        && x.Status == "Active" && (x.EndsAtUtc == null || x.EndsAtUtc > DateTimeOffset.UtcNow), ct)
                },
                closings = closingRows.Select(x => new
                {
                    x.Id,
                    x.BusinessDate,
                    x.Status,
                    x.NetSales,
                    x.PaymentsIn,
                    x.RefundsOut,
                    x.CashVariance
                })
            });
        }).RequireAuthorization("reports.sales");
        return endpoints;
    }
}
