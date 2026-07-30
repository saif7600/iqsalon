using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class InventoryService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> PostMovement(PostStockMovementRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || request.Direction is not ("Inbound" or "Outbound"))
            return CommercialResult.Fail("validation", "Branch, direction, positive quantity, and idempotency key are required.");
        var replay = await db.StockMovements.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == request.ProductId
            && x.OrganizationId == request.OrganizationId && x.IsActive && x.TrackInventory, ct);
        var location = await db.InventoryLocations.SingleOrDefaultAsync(x => x.Id == request.InventoryLocationId
            && x.BranchId == request.BranchId && x.IsActive, ct);
        if (product is null || location is null) return CommercialResult.Fail("scope", "Tracked product or location is unavailable.");
        if (request.Direction == "Outbound" && (location.IsQuarantine || !location.IsSellable && !location.IsConsumable))
            return CommercialResult.Fail("location", "Location cannot issue stock.");
        ProductBatch? batch = null;
        if (product.TrackBatches)
        {
            if (request.BatchId is null) return CommercialResult.Fail("batch", "Batch is required.");
            batch = await db.ProductBatches.SingleOrDefaultAsync(x => x.Id == request.BatchId
                && x.ProductId == product.Id && x.BranchId == request.BranchId, ct);
            if (batch is null || request.Direction == "Outbound"
                && (batch.Status != "Active" || batch.ExpiresAtUtc <= DateTimeOffset.UtcNow))
                return CommercialResult.Fail("batch", "Batch is unavailable, quarantined, or expired.");
        }
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(x =>
            x.BranchId == request.BranchId && x.InventoryLocationId == request.InventoryLocationId
            && x.ProductId == product.Id && x.BatchId == request.BatchId, ct);
        balance ??= new InventoryBalance
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            InventoryLocationId = request.InventoryLocationId,
            ProductId = product.Id,
            BatchId = request.BatchId
        };
        if (balance.Id != Guid.Empty && db.Entry(balance).State == EntityState.Detached) db.InventoryBalances.Add(balance);
        var settings = await db.InventoryCostSettings.SingleOrDefaultAsync(x => x.OrganizationId == request.OrganizationId, ct)
            ?? new InventoryCostSettings { TenantId = tenant.TenantId.Value, OrganizationId = request.OrganizationId };
        if (db.Entry(settings).State == EntityState.Detached) db.InventoryCostSettings.Add(settings);
        var unitCost = request.Direction == "Inbound"
            ? InventoryRules.RoundCost(request.UnitCost ?? product.LastPurchaseCost, settings.CostRoundingPrecision)
            : InventoryRules.OutboundCost(settings.CostingMethod, balance.AverageUnitCost, product.StandardCost);
        var signedQuantity = request.Direction == "Inbound" ? request.Quantity : -request.Quantity;
        var resultingQuantity = balance.QuantityOnHand + signedQuantity;
        if (resultingQuantity < 0 && !(product.AllowNegativeStock || settings.AllowNegativeStock))
            return CommercialResult.Fail("stock", "Insufficient available stock.");
        var nextAverage = request.Direction == "Inbound"
            ? InventoryRules.WeightedAverage(balance.QuantityOnHand, balance.AverageUnitCost, request.Quantity, unitCost,
                settings.CostRoundingPrecision)
            : balance.AverageUnitCost;
        var movement = new StockMovement
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            InventoryLocationId = request.InventoryLocationId,
            ProductId = product.Id,
            BatchId = request.BatchId,
            MovementType = request.MovementType,
            Direction = request.Direction,
            QuantityBaseUnit = request.Quantity,
            UnitCost = unitCost,
            TotalCost = InventoryRules.RoundCost(request.Quantity * unitCost, settings.CostRoundingPrecision),
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ReferenceNumber = request.ReferenceNumber,
            ReasonCode = request.ReasonCode,
            Notes = request.Notes?.Trim(),
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedByUserId = tenant.UserId.Value,
            IdempotencyKey = request.IdempotencyKey,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")
        };
        db.StockMovements.Add(movement);
        if (db.Entry(balance).State == EntityState.Detached) db.InventoryBalances.Add(balance);
        balance.QuantityOnHand = resultingQuantity;
        balance.AverageUnitCost = nextAverage;
        balance.LastMovementAtUtc = movement.OccurredAtUtc;
        balance.Version++;
        if (batch is not null)
        {
            batch.RemainingQuantity += signedQuantity;
            if (batch.RemainingQuantity < 0) return CommercialResult.Fail("batch", "Insufficient batch stock.");
            if (batch.RemainingQuantity == 0) batch.Status = "Depleted";
        }
        product.AverageCost = nextAverage;
        if (request.Direction == "Inbound") product.LastPurchaseCost = unitCost;
        Audit(request.OrganizationId, "inventory.movement.posted", movement.Id);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return CommercialResult.Success(movement.Id, movement.Id.ToString());
    }

    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "StockMovement",
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public static class InventoryRules
{
    public static decimal WeightedAverage(decimal existingQuantity, decimal existingCost,
        decimal receiptQuantity, decimal receiptCost, int precision)
    {
        if (receiptQuantity <= 0 || existingQuantity < 0) throw new ArgumentOutOfRangeException(nameof(receiptQuantity));
        var totalQuantity = existingQuantity + receiptQuantity;
        return RoundCost((existingQuantity * existingCost + receiptQuantity * receiptCost) / totalQuantity, precision);
    }
    public static decimal OutboundCost(string method, decimal averageCost, decimal standardCost) =>
        method == "StandardCost" ? standardCost : averageCost;
    public static decimal RoundCost(decimal value, int precision) =>
        Math.Round(value, Math.Clamp(precision, 2, 6), MidpointRounding.AwayFromZero);
}

public sealed record PostStockMovementRequest(Guid OrganizationId, Guid BranchId,
    Guid InventoryLocationId, Guid ProductId, Guid? BatchId, string MovementType,
    string Direction, decimal Quantity, decimal? UnitCost, string ReferenceType,
    Guid? ReferenceId, string? ReferenceNumber, string ReasonCode, string? Notes,
    string IdempotencyKey, string? CorrelationId = null);
