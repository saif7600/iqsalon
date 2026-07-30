using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class ConsumptionService(AppDbContext db, TenantContext tenant, InventoryService inventory)
{
    public async Task<CommercialResult> ConsumeAppointment(Guid appointmentId, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null) return CommercialResult.Fail("unauthorized", "Tenant user is required.");
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == appointmentId, ct);
        if (appointment is null || !tenant.CanAccessBranch(appointment.BranchId))
            return CommercialResult.Fail("not_found", "Appointment not found.");
        var key = $"appointment-consumption:{appointment.Id:N}";
        var replay = await db.AppointmentConsumptions.SingleOrDefaultAsync(x => x.IdempotencyKey == key, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);
        var ownTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownTransaction ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var location = await db.InventoryLocations.Where(x => x.BranchId == appointment.BranchId && x.IsActive
            && x.IsConsumable && !x.IsQuarantine).OrderBy(x => x.Code).FirstOrDefaultAsync(ct);
        if (location is null) return CommercialResult.Fail("location", "A consumable inventory location is required.");
        var appointmentLines = await db.AppointmentServices.Where(x => x.AppointmentId == appointment.Id).ToListAsync(ct);
        var serviceIds = appointmentLines.Select(x => x.ServiceId).Distinct().ToArray();
        var recipes = await db.ServiceRecipes.Where(x => serviceIds.Contains(x.ServiceId) && x.Status == "Active"
            && (x.EffectiveFromUtc == null || x.EffectiveFromUtc <= DateTimeOffset.UtcNow)
            && (x.EffectiveToUtc == null || x.EffectiveToUtc > DateTimeOffset.UtcNow))
            .OrderByDescending(x => x.VersionNumber).ToListAsync(ct);
        var selected = recipes.GroupBy(x => x.ServiceId).ToDictionary(x => x.Key, x => x.First());
        if (serviceIds.Any(x => !selected.ContainsKey(x)))
            return CommercialResult.Fail("recipe", "Every appointment service requires an active recipe.");
        var recipeIds = selected.Values.Select(x => x.Id).ToArray();
        var recipeLines = await db.ServiceRecipeLines.Where(x => recipeIds.Contains(x.ServiceRecipeId)).OrderBy(x => x.Sequence).ToListAsync(ct);
        var consumption = new AppointmentConsumption
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = appointment.OrganizationId,
            BranchId = appointment.BranchId,
            AppointmentId = appointment.Id,
            IdempotencyKey = key,
            PostedByUserId = tenant.UserId.Value
        };
        db.AppointmentConsumptions.Add(consumption);
        foreach (var appointmentLine in appointmentLines)
        {
            var recipe = selected[appointmentLine.ServiceId];
            foreach (var recipeLine in recipeLines.Where(x => x.ServiceRecipeId == recipe.Id))
            {
                var required = ConsumptionRules.WithWastage(recipeLine.QuantityBaseUnit, recipeLine.WastageAllowancePercent);
                var product = await db.Products.SingleAsync(x => x.Id == recipeLine.ProductId, ct);
                var allocations = await Allocate(location.Id, appointment.BranchId, product, required, ct);
                if (allocations is null) return CommercialResult.Fail("stock", $"Insufficient stock for product {product.Sku}.");
                foreach (var allocation in allocations)
                {
                    var movement = await inventory.PostMovement(new PostStockMovementRequest(
                        appointment.OrganizationId, appointment.BranchId, location.Id, product.Id, allocation.BatchId,
                        "ServiceConsumption", "Outbound", allocation.Quantity, null, "AppointmentConsumption",
                        consumption.Id, appointment.AppointmentNumber, "ServiceRecipe", null,
                        $"{key}:{appointmentLine.Id:N}:{recipeLine.Id:N}:{allocation.BatchId?.ToString("N") ?? "none"}"), ct);
                    if (!movement.IsSuccess || movement.Id is null) return movement;
                    var posted = await db.StockMovements.SingleAsync(x => x.Id == movement.Id, ct);
                    db.AppointmentConsumptionLines.Add(new AppointmentConsumptionLine
                    {
                        TenantId = tenant.TenantId.Value,
                        OrganizationId = appointment.OrganizationId,
                        AppointmentConsumptionId = consumption.Id,
                        AppointmentServiceId = appointmentLine.Id,
                        ServiceRecipeLineId = recipeLine.Id,
                        ProductId = product.Id,
                        InventoryLocationId = location.Id,
                        BatchId = allocation.BatchId,
                        QuantityBaseUnit = allocation.Quantity,
                        UnitCost = posted.UnitCost,
                        StockMovementId = posted.Id
                    });
                }
            }
        }
        Audit(appointment.OrganizationId, "appointment_consumption.posted", consumption.Id);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return CommercialResult.Success(consumption.Id, consumption.Id.ToString());
    }

    public async Task<CommercialResult> Reverse(Guid id, string reason, CancellationToken ct)
    {
        var consumption = await db.AppointmentConsumptions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (consumption is null || !tenant.CanAccessBranch(consumption.BranchId))
            return CommercialResult.Fail("not_found", "Consumption not found.");
        if (consumption.Status != "Posted" || string.IsNullOrWhiteSpace(reason))
            return CommercialResult.Fail("status", "Posted consumption and a reason are required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var lines = await db.AppointmentConsumptionLines.Where(x => x.AppointmentConsumptionId == id).ToListAsync(ct);
        foreach (var line in lines)
        {
            var result = await inventory.PostMovement(new PostStockMovementRequest(
                consumption.OrganizationId, consumption.BranchId, line.InventoryLocationId, line.ProductId, line.BatchId,
                "ConsumptionReversal", "Inbound", line.QuantityBaseUnit, line.UnitCost, "AppointmentConsumption",
                consumption.Id, consumption.Id.ToString(), "Reversal", reason,
                $"consumption-reversal:{consumption.Id:N}:{line.Id:N}"), ct);
            if (!result.IsSuccess) return result;
        }
        consumption.Status = "Reversed"; consumption.ReversedByUserId = tenant.UserId;
        consumption.ReversedAtUtc = DateTimeOffset.UtcNow; consumption.ReversalReason = reason.Trim();
        Audit(consumption.OrganizationId, "appointment_consumption.reversed", consumption.Id);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return CommercialResult.Success(consumption.Id, consumption.Id.ToString());
    }

    private async Task<List<Allocation>?> Allocate(Guid locationId, Guid branchId, Product product, decimal required, CancellationToken ct)
    {
        if (!product.TrackBatches)
        {
            var balance = await db.InventoryBalances.SingleOrDefaultAsync(x => x.BranchId == branchId
                && x.InventoryLocationId == locationId && x.ProductId == product.Id && x.BatchId == null, ct);
            return balance is not null && balance.QuantityAvailable >= required ? [new(null, required)] : null;
        }
        var balances = await db.InventoryBalances.Where(x => x.BranchId == branchId && x.InventoryLocationId == locationId
            && x.ProductId == product.Id && x.BatchId != null && x.QuantityOnHand > x.QuantityReserved)
            .Join(db.ProductBatches.Where(x => x.Status == "Active" && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTimeOffset.UtcNow)),
                balance => balance.BatchId, batch => batch.Id, (balance, batch) => new { balance, batch })
            .OrderBy(x => x.batch.ExpiresAtUtc).ThenBy(x => x.batch.ReceivedAtUtc).ToListAsync(ct);
        var remaining = required; var result = new List<Allocation>();
        foreach (var item in balances)
        {
            var quantity = Math.Min(remaining, item.balance.QuantityAvailable);
            if (quantity > 0) result.Add(new(item.batch.Id, quantity));
            remaining -= quantity;
            if (remaining <= 0) return result;
        }
        return null;
    }

    private void Audit(Guid organizationId, string action, Guid id) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "AppointmentConsumption",
        EntityId = id.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
    private sealed record Allocation(Guid? BatchId, decimal Quantity);
}

public static class ConsumptionRules
{
    public static decimal WithWastage(decimal quantity, decimal wastagePercent)
    {
        if (quantity <= 0 || wastagePercent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(quantity));
        return InventoryRules.RoundCost(quantity * (1 + wastagePercent / 100m), 6);
    }
}
