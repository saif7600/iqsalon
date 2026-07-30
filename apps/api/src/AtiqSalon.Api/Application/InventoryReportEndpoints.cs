using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class InventoryReportEndpoints
{
    public static IEndpointRouteBuilder MapInventoryReportApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/reports/inventory", async (Guid branchId, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (!tenant.CanAccessBranch(branchId)) return Results.Forbid();
            var balances = await db.InventoryBalances.Where(x => x.BranchId == branchId).ToListAsync(ct);
            var products = await db.Products.Where(x => balances.Select(y => y.ProductId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
            var wastage = await db.WastageRecords.Where(x => x.BranchId == branchId && x.Status == "Posted").SumAsync(x => (decimal?)x.Quantity, ct) ?? 0;
            var expenses = await db.ExpenseRecords.Where(x => x.BranchId == branchId && x.Status != "Draft").SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;
            var receipts = await db.GoodsReceipts.CountAsync(x => x.BranchId == branchId && x.Status == "Posted", ct);
            return Results.Ok(new
            {
                quantityOnHand = balances.Sum(x => x.QuantityOnHand),
                quantityReserved = balances.Sum(x => x.QuantityReserved),
                inventoryValue = balances.Sum(x => x.QuantityOnHand * x.AverageUnitCost),
                lowStockProducts = balances.Count(x => products.TryGetValue(x.ProductId, out var product)
                    && x.QuantityAvailable <= product.ReorderPoint),
                wastageQuantity = wastage,
                approvedExpenses = expenses,
                postedReceipts = receipts,
                balances = balances.Select(x => new
                {
                    x.ProductId,
                    product = products.GetValueOrDefault(x.ProductId)?.Name ?? x.ProductId.ToString(),
                    x.InventoryLocationId,
                    x.BatchId,
                    x.QuantityOnHand,
                    x.QuantityReserved,
                    x.QuantityAvailable,
                    x.AverageUnitCost,
                    value = x.QuantityOnHand * x.AverageUnitCost
                })
            });
        }).RequireAuthorization("reports.inventory");
        return endpoints;
    }
}
