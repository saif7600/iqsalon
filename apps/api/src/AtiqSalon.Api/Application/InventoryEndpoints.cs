using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/units-of-measure", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.UnitsOfMeasure.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct)))
            .RequireAuthorization("inventory.read");
        api.MapPost("/units-of-measure", async (UnitOfMeasure unit, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || unit.DecimalPrecision is < 0 or > 6
                || string.IsNullOrWhiteSpace(unit.Code) || string.IsNullOrWhiteSpace(unit.Name))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["unit"] = ["Code, name, and precision from 0 to 6 are required."] });
            unit.TenantId = tenant.TenantId.Value;
            unit.Code = unit.Code.Trim().ToUpperInvariant();
            unit.Name = unit.Name.Trim();
            db.UnitsOfMeasure.Add(unit);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/units-of-measure/{unit.Id}", unit);
        }).RequireAuthorization("inventory.manage");
        api.MapGet("/inventory-locations", async (Guid branchId, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
            tenant.CanAccessBranch(branchId)
                ? Results.Ok(await db.InventoryLocations.Where(x => x.BranchId == branchId && x.IsActive)
                    .OrderBy(x => x.Name).ToListAsync(ct))
                : Results.Forbid()).RequireAuthorization("inventory.read");
        api.MapPost("/inventory-locations", async (InventoryLocation location, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || !tenant.CanAccessBranch(location.BranchId))
                return Results.Forbid();
            location.TenantId = tenant.TenantId.Value;
            location.Code = location.Code.Trim().ToUpperInvariant();
            location.Name = location.Name.Trim();
            db.InventoryLocations.Add(location);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/inventory-locations/{location.Id}", location);
        }).RequireAuthorization("inventory.manage");
        api.MapGet("/inventory/balances", async (Guid branchId, Guid? locationId, Guid? productId,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (!tenant.CanAccessBranch(branchId)) return Results.Forbid();
            var query = db.InventoryBalances.Where(x => x.BranchId == branchId);
            if (locationId.HasValue) query = query.Where(x => x.InventoryLocationId == locationId);
            if (productId.HasValue) query = query.Where(x => x.ProductId == productId);
            return Results.Ok(await query.OrderBy(x => x.ProductId).ToListAsync(ct));
        }).RequireAuthorization("inventory.read");
        api.MapGet("/inventory/movements", async (Guid branchId, Guid? productId,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (!tenant.CanAccessBranch(branchId)) return Results.Forbid();
            var query = db.StockMovements.Where(x => x.BranchId == branchId);
            if (productId.HasValue) query = query.Where(x => x.ProductId == productId);
            return Results.Ok(await query.OrderByDescending(x => x.OccurredAtUtc).Take(1000).ToListAsync(ct));
        }).RequireAuthorization("inventory.movements.read");
        api.MapPost("/inventory/movements", async (PostStockMovementRequest request,
            InventoryService service, CancellationToken ct) =>
        {
            var result = await service.PostMovement(request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("inventory.adjust");
        return endpoints;
    }
}
