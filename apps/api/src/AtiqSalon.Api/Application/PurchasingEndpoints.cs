using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class PurchasingEndpoints
{
    public static IEndpointRouteBuilder MapPurchasingApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/suppliers", async (Guid organizationId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Suppliers.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Name).ToListAsync(ct))).RequireAuthorization("suppliers.read");
        api.MapPost("/suppliers", async (Supplier supplier, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || string.IsNullOrWhiteSpace(supplier.Name))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplier"] = ["Organization and name are required."] });
            supplier.TenantId = tenant.TenantId.Value; supplier.Name = supplier.Name.Trim();
            supplier.SupplierNumber = $"SUP-{await db.Suppliers.CountAsync(x => x.OrganizationId == supplier.OrganizationId, ct) + 1:000000}";
            supplier.Email = supplier.Email?.Trim().ToLowerInvariant();
            db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/suppliers/{supplier.Id}", supplier);
        }).RequireAuthorization("suppliers.create");
        api.MapPost("/suppliers/{id:guid}/block", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (supplier is null) return Results.NotFound();
            supplier.Status = "Blocked"; await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("suppliers.block");
        api.MapGet("/purchase-orders", async (Guid branchId, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
            tenant.CanAccessBranch(branchId) ? Results.Ok(await db.PurchaseOrders.Where(x => x.BranchId == branchId).OrderByDescending(x => x.OrderDate).ToListAsync(ct)) : Results.Forbid()).RequireAuthorization("purchase_orders.read");
        api.MapGet("/purchase-orders/{id:guid}", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct);
            return order is null || !tenant.CanAccessBranch(order.BranchId) ? Results.NotFound()
                : Results.Ok(new { order, lines = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == id).OrderBy(x => x.Sequence).ToListAsync(ct) });
        }).RequireAuthorization("purchase_orders.read");
        api.MapPost("/purchase-orders", async (CreatePurchaseOrderRequest request, PurchasingService service, CancellationToken ct) =>
        {
            var result = await service.CreateOrder(request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/purchase-orders/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("purchase_orders.create");
        api.MapPost("/purchase-orders/{id:guid}/approve", async (Guid id, PurchasingService service, CancellationToken ct) =>
        {
            var result = await service.ApproveOrder(id, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("purchase_orders.approve");
        api.MapGet("/goods-receipts", async (Guid branchId, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
            tenant.CanAccessBranch(branchId) ? Results.Ok(await db.GoodsReceipts.Where(x => x.BranchId == branchId).OrderByDescending(x => x.ReceiptDate).ToListAsync(ct)) : Results.Forbid()).RequireAuthorization("goods_receipts.read");
        api.MapPost("/goods-receipts/post", async (PostGoodsReceiptRequest request, PurchasingService service, CancellationToken ct) =>
        {
            var result = await service.PostReceipt(request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("goods_receipts.post");
        return endpoints;
    }
}
