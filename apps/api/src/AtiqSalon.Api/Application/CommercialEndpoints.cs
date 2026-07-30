using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class CommercialEndpoints
{
    public static IEndpointRouteBuilder MapCommercialApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/tax-codes", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TaxCodes.OrderBy(x => x.Code).ToListAsync(ct))).RequireAuthorization("tax.read");
        api.MapGet("/products", async (Guid? branchId, string? search, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Products.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(x => x.Name.ToLower().Contains(term) || x.Sku.ToLower().Contains(term)
                    || x.Barcode != null && x.Barcode == term);
            }
            if (branchId.HasValue)
                query = query.Where(x => db.BranchProducts.Any(y => y.ProductId == x.Id && y.BranchId == branchId
                    && y.IsActive && y.IsAvailableForSale));
            return Results.Ok(await query.OrderBy(x => x.Name).Take(100).ToListAsync(ct));
        }).RequireAuthorization("products.read");
        api.MapPost("/products", async (Product item, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (item.RetailPrice < 0 || item.CostPrice < 0 || string.IsNullOrWhiteSpace(item.Sku))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = ["SKU and non-negative prices are required."] });
            item.TenantId = tenant.TenantId!.Value;
            item.Sku = item.Sku.Trim().ToUpperInvariant();
            db.Products.Add(item);
            await Audit(db, tenant, item.OrganizationId, "product.created", item.Id, ct);
            return Results.Created($"/api/v1/products/{item.Id}", item);
        }).RequireAuthorization("products.create");
        api.MapPost("/sales", async (CreateSaleRequest request, CommercialService service, CancellationToken ct) =>
        {
            var result = await service.CreateSale(request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/sales/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("pos.create_sale");
        api.MapGet("/sales", async (Guid? branchId, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid();
            var query = db.Sales.AsQueryable();
            if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId));
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            return Results.Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(ct));
        }).RequireAuthorization("pos.access");
        api.MapGet("/sales/{id:guid}", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
            await db.Sales.SingleOrDefaultAsync(x => x.Id == id, ct) is { } sale && tenant.CanAccessBranch(sale.BranchId)
                ? Results.Ok(new
                {
                    sale,
                    lines = await db.SaleLines.Where(x => x.SaleId == id).OrderBy(x => x.Sequence).ToListAsync(ct),
                    payments = await db.PaymentAllocations.Where(x => x.SaleId == id)
                        .Join(db.Payments, x => x.PaymentId, x => x.Id, (allocation, payment) => new { payment, allocation.Amount }).ToListAsync(ct),
                    invoice = await db.Invoices.SingleOrDefaultAsync(x => x.SaleId == id, ct)
                })
                : Results.NotFound()).RequireAuthorization("pos.access");
        api.MapPost("/sales/{id:guid}/payments", async (Guid id, RecordPaymentRequest request,
            CommercialService service, CancellationToken ct) =>
        {
            var result = await service.RecordPayment(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("payments.record");
        api.MapPost("/sales/{id:guid}/post", async (Guid id, PostSaleRequest request,
            CommercialService service, CancellationToken ct) =>
        {
            var result = await service.PostSale(id, request.IdempotencyKey, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("pos.post_sale");
        api.MapGet("/payment-methods", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.PaymentMethods.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(ct)))
            .RequireAuthorization("payments.read");
        api.MapPost("/payment-methods", async (PaymentMethod method, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || string.IsNullOrWhiteSpace(method.Code) || string.IsNullOrWhiteSpace(method.Name))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["method"] = ["Code and name are required."] });
            method.TenantId = tenant.TenantId.Value;
            method.Code = method.Code.Trim().ToUpperInvariant();
            method.Name = method.Name.Trim();
            db.PaymentMethods.Add(method);
            await Audit(db, tenant, method.OrganizationId, "payment_method.created", method.Id, ct);
            return Results.Created($"/api/v1/payment-methods/{method.Id}", method);
        }).RequireAuthorization("payments.manage");
        api.MapPost("/sales/{id:guid}/refunds", async (Guid id, RefundSaleRequest request,
            CommercialControlsService service, CancellationToken ct) =>
        {
            var result = await service.RefundSale(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("refunds.create");
        api.MapGet("/sales/{id:guid}/refunds", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Refunds.Where(x => x.SaleId == id).OrderByDescending(x => x.RefundedAtUtc).ToListAsync(ct)))
            .RequireAuthorization("refunds.read");
        api.MapPost("/sales/{id:guid}/discount-approvals", async (Guid id, RequestDiscountApproval request,
            CommercialControlsService service, CancellationToken ct) =>
        {
            var result = await service.RequestDiscount(id, request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/discount-approvals/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("discounts.apply");
        api.MapPost("/discount-approvals/{id:guid}/decision", async (Guid id, DecideDiscountApproval request,
            CommercialControlsService service, CancellationToken ct) =>
        {
            var result = await service.DecideDiscount(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("discounts.approve");
        api.MapPost("/till-sessions/open", async (OpenTillRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
                return Results.Forbid();
            if (await db.TillSessions.AnyAsync(x => x.BranchId == request.BranchId
                && x.OpenedByUserId == tenant.UserId && x.Status == "Open", ct))
                return Results.Conflict(new { message = "The user already has an open till." });
            var till = new TillSession
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                BranchId = request.BranchId,
                OpenedByUserId = tenant.UserId.Value,
                OpeningFloat = request.OpeningFloat,
                ExpectedCash = request.OpeningFloat
            };
            db.TillSessions.Add(till);
            await Audit(db, tenant, till.OrganizationId, "till.opened", till.Id, ct);
            return Results.Created($"/api/v1/till-sessions/{till.Id}", till);
        }).RequireAuthorization("tills.open");
        api.MapGet("/till-sessions/current", async (Guid branchId, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
            tenant.CanAccessBranch(branchId)
                ? Results.Ok(await db.TillSessions.Where(x => x.BranchId == branchId && x.Status == "Open")
                    .OrderByDescending(x => x.OpenedAtUtc).FirstOrDefaultAsync(ct))
                : Results.Forbid()).RequireAuthorization("tills.read");
        api.MapPost("/till-sessions/{id:guid}/close", async (Guid id, CloseTillRequest request,
            CommercialControlsService service, CancellationToken ct) =>
        {
            var result = await service.CloseTill(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("tills.close");
        return endpoints;
    }

    private static async Task Audit(AppDbContext db, TenantContext tenant, Guid organizationId,
        string action, Guid entityId, CancellationToken ct)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId,
            ActorUserId = tenant.UserId,
            Action = action,
            EntityType = action.Split('.')[0],
            EntityId = entityId.ToString(),
            Source = "api",
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed record PostSaleRequest(string IdempotencyKey);
public sealed record OpenTillRequest(Guid OrganizationId, Guid BranchId, decimal OpeningFloat);
