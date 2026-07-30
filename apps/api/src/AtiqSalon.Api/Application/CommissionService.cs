using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class CommissionService(AppDbContext db)
{
    public async Task GenerateForPostedSale(Sale sale, CancellationToken ct)
    {
        var lines = await db.SaleLines.Where(x => x.SaleId == sale.Id
            && x.AssignedStaffMemberId != null && x.CommissionEligible).ToListAsync(ct);
        foreach (var line in lines)
        {
            var assignment = await db.StaffCommissionAssignments
                .Where(x => x.StaffMemberId == line.AssignedStaffMemberId
                    && x.BranchId == sale.BranchId && x.IsActive
                    && x.EffectiveFrom <= sale.BusinessDate
                    && (x.EffectiveTo == null || x.EffectiveTo >= sale.BusinessDate))
                .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
            if (assignment is null) continue;
            var plan = await db.CommissionPlans.SingleAsync(x =>
                x.Id == assignment.CommissionPlanId && x.IsActive, ct);
            var key = $"sale:{sale.Id}:line:{line.Id}";
            if (await db.CommissionLedgerEntries.AnyAsync(x => x.IdempotencyKey == key, ct)) continue;
            var rate = line.LineType == "Product"
                ? plan.ProductRatePercentage
                : plan.ServiceRatePercentage;
            var basis = CommissionRules.Basis(line, plan.Basis);
            db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
            {
                TenantId = sale.TenantId,
                OrganizationId = sale.OrganizationId,
                BranchId = sale.BranchId,
                StaffMemberId = line.AssignedStaffMemberId!.Value,
                CommissionPlanId = plan.Id,
                SaleId = sale.Id,
                SaleLineId = line.Id,
                EntryType = "Earned",
                Basis = plan.Basis,
                BasisAmount = basis,
                RatePercentage = rate,
                Amount = CommissionRules.Calculate(basis, rate),
                IdempotencyKey = key,
                BusinessDate = sale.BusinessDate
            });
        }
    }

    public async Task ReverseForRefund(Sale sale, Refund refund, CancellationToken ct)
    {
        var earned = await db.CommissionLedgerEntries.Where(x =>
            x.SaleId == sale.Id && x.EntryType == "Earned").ToListAsync(ct);
        if (earned.Count == 0 || sale.GrandTotal <= 0) return;
        var ratio = Math.Min(1m, refund.Amount / sale.GrandTotal);
        foreach (var original in earned)
        {
            var key = $"refund:{refund.Id}:commission:{original.Id}";
            if (await db.CommissionLedgerEntries.AnyAsync(x => x.IdempotencyKey == key, ct)) continue;
            db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
            {
                TenantId = original.TenantId,
                OrganizationId = original.OrganizationId,
                BranchId = original.BranchId,
                StaffMemberId = original.StaffMemberId,
                CommissionPlanId = original.CommissionPlanId,
                SaleId = original.SaleId,
                SaleLineId = original.SaleLineId,
                RefundId = refund.Id,
                EntryType = "Reversal",
                Basis = original.Basis,
                BasisAmount = -CommercialRules.Round(original.BasisAmount * ratio),
                RatePercentage = original.RatePercentage,
                Amount = -CommercialRules.Round(original.Amount * ratio),
                IdempotencyKey = key,
                BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        }
    }
}

public static class CommissionRules
{
    public static decimal Basis(SaleLine line, string basis) => basis switch
    {
        "GrossRevenue" => line.GrossAmount,
        "GrossProfit" => Math.Max(0, line.NetAmount - line.CostSnapshot * line.Quantity),
        _ => line.NetAmount
    };

    public static decimal Calculate(decimal basis, decimal ratePercentage) =>
        CommercialRules.Round(Math.Max(0, basis) * Math.Max(0, ratePercentage) / 100m);
}
