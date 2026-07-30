using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class InventoryControlService(AppDbContext db, TenantContext tenant, InventoryService inventory)
{
    public async Task<CommercialResult> CreateStocktake(CreateStocktakeRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
            return CommercialResult.Fail("branch", "Branch access is required.");
        var balances = await db.InventoryBalances.Where(x => x.BranchId == request.BranchId
            && x.InventoryLocationId == request.InventoryLocationId).ToListAsync(ct);
        var item = new Stocktake
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            InventoryLocationId = request.InventoryLocationId,
            StocktakeNumber = $"STK-{await db.Stocktakes.CountAsync(x => x.OrganizationId == request.OrganizationId, ct) + 1:000000}",
            BusinessDate = request.BusinessDate,
            Notes = request.Notes?.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        db.Stocktakes.Add(item);
        db.StocktakeLines.AddRange(balances.Select(x => new StocktakeLine
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            StocktakeId = item.Id,
            ProductId = x.ProductId,
            BatchId = x.BatchId,
            SystemQuantity = x.QuantityOnHand
        }));
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(item.Id, item.StocktakeNumber);
    }

    public async Task<CommercialResult> Count(Guid id, IReadOnlyCollection<StocktakeCountRequest> counts, CancellationToken ct)
    {
        var item = await db.Stocktakes.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Draft", ct);
        if (item is null || !tenant.CanAccessBranch(item.BranchId)) return CommercialResult.Fail("status", "Draft stocktake not found.");
        var lines = await db.StocktakeLines.Where(x => x.StocktakeId == id).ToDictionaryAsync(x => x.Id, ct);
        if (counts.Count != lines.Count || counts.Any(x => x.Quantity < 0 || !lines.ContainsKey(x.LineId)))
            return CommercialResult.Fail("count", "Every line requires a non-negative count.");
        foreach (var count in counts) { var line = lines[count.LineId]; line.CountedQuantity = count.Quantity; line.VarianceQuantity = count.Quantity - line.SystemQuantity; }
        item.Status = "Counted"; await db.SaveChangesAsync(ct);
        return CommercialResult.Success(item.Id, item.StocktakeNumber);
    }

    public async Task<CommercialResult> PostStocktake(Guid id, CancellationToken ct)
    {
        var item = await db.Stocktakes.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Counted", ct);
        if (item is null || tenant.UserId is null || !tenant.CanAccessBranch(item.BranchId))
            return CommercialResult.Fail("status", "Counted stocktake not found.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var lines = await db.StocktakeLines.Where(x => x.StocktakeId == id && x.VarianceQuantity != 0).ToListAsync(ct);
        foreach (var line in lines)
        {
            var inbound = line.VarianceQuantity > 0;
            var result = await inventory.PostMovement(new(item.OrganizationId, item.BranchId, item.InventoryLocationId,
                line.ProductId, line.BatchId, "StocktakeAdjustment", inbound ? "Inbound" : "Outbound",
                Math.Abs(line.VarianceQuantity), null, "Stocktake", item.Id, item.StocktakeNumber,
                "PhysicalCount", item.Notes, $"stocktake:{item.Id:N}:{line.Id:N}"), ct);
            if (!result.IsSuccess) return result;
        }
        item.Status = "Posted"; item.ApprovedByUserId = tenant.UserId; item.PostedByUserId = tenant.UserId;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return CommercialResult.Success(item.Id, item.StocktakeNumber);
    }

    public async Task<CommercialResult> CreateWastage(CreateWastageRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.ReasonCode))
            return CommercialResult.Fail("validation", "Branch, quantity, and reason are required.");
        var item = new WastageRecord
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            InventoryLocationId = request.InventoryLocationId,
            ProductId = request.ProductId,
            BatchId = request.BatchId,
            WastageNumber = $"WST-{await db.WastageRecords.CountAsync(x => x.OrganizationId == request.OrganizationId, ct) + 1:000000}",
            Quantity = request.Quantity,
            ReasonCode = request.ReasonCode.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        db.WastageRecords.Add(item); await db.SaveChangesAsync(ct);
        return CommercialResult.Success(item.Id, item.WastageNumber);
    }

    public async Task<CommercialResult> PostWastage(Guid id, CancellationToken ct)
    {
        var item = await db.WastageRecords.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Draft", ct);
        if (item is null || tenant.UserId is null || !tenant.CanAccessBranch(item.BranchId))
            return CommercialResult.Fail("status", "Draft wastage not found.");
        var result = await inventory.PostMovement(new(item.OrganizationId, item.BranchId, item.InventoryLocationId,
            item.ProductId, item.BatchId, "Wastage", "Outbound", item.Quantity, null, "Wastage", item.Id,
            item.WastageNumber, item.ReasonCode, item.Notes, $"wastage:{item.Id:N}"), ct);
        if (!result.IsSuccess) return result;
        item.Status = "Posted"; item.ApprovedByUserId = tenant.UserId; item.PostedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct); return CommercialResult.Success(item.Id, item.WastageNumber);
    }
}

public sealed record CreateStocktakeRequest(Guid OrganizationId, Guid BranchId, Guid InventoryLocationId, DateOnly BusinessDate, string? Notes);
public sealed record StocktakeCountRequest(Guid LineId, decimal Quantity);
public sealed record CreateWastageRequest(Guid OrganizationId, Guid BranchId, Guid InventoryLocationId,
    Guid ProductId, Guid? BatchId, decimal Quantity, string ReasonCode, string? Notes);
