using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class InventoryControlEndpoints
{
    public static IEndpointRouteBuilder MapInventoryControlApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapPost("/stocktakes", async (CreateStocktakeRequest r, InventoryControlService s, CancellationToken ct) => Result(await s.CreateStocktake(r, ct))).RequireAuthorization("stocktakes.create");
        api.MapPut("/stocktakes/{id:guid}/count", async (Guid id, StocktakeCountRequest[] r, InventoryControlService s, CancellationToken ct) => Result(await s.Count(id, r, ct))).RequireAuthorization("stocktakes.count");
        api.MapPost("/stocktakes/{id:guid}/post", async (Guid id, InventoryControlService s, CancellationToken ct) => Result(await s.PostStocktake(id, ct))).RequireAuthorization("stocktakes.post");
        api.MapPost("/wastage", async (CreateWastageRequest r, InventoryControlService s, CancellationToken ct) => Result(await s.CreateWastage(r, ct))).RequireAuthorization("wastage.record");
        api.MapPost("/wastage/{id:guid}/post", async (Guid id, InventoryControlService s, CancellationToken ct) => Result(await s.PostWastage(id, ct))).RequireAuthorization("wastage.approve");
        api.MapGet("/expenses", async (Guid branchId, TenantContext t, AppDbContext db, CancellationToken ct) =>
            !t.CanAccessBranch(branchId) ? Results.Forbid() : Results.Ok(await db.ExpenseRecords.Where(x => x.BranchId == branchId).OrderByDescending(x => x.ExpenseDate).ToListAsync(ct))).RequireAuthorization("expenses.read");
        api.MapPost("/expenses", async (ExpenseRecord x, TenantContext t, AppDbContext db, CancellationToken ct) =>
        {
            if (t.TenantId is null || t.UserId is null || !t.CanAccessBranch(x.BranchId) || x.NetAmount < 0 || x.TaxAmount < 0) return Results.BadRequest();
            x.TenantId = t.TenantId.Value; x.CreatedByUserId = t.UserId.Value; x.TotalAmount = x.NetAmount + x.TaxAmount;
            x.ExpenseNumber = $"EXP-{await db.ExpenseRecords.CountAsync(y => y.OrganizationId == x.OrganizationId, ct) + 1:000000}";
            db.ExpenseRecords.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/expenses/{x.Id}", x);
        }).RequireAuthorization("expenses.create");
        api.MapPost("/expenses/{id:guid}/approve", async (Guid id, TenantContext t, AppDbContext db, CancellationToken ct) =>
        {
            var x = await db.ExpenseRecords.SingleOrDefaultAsync(y => y.Id == id && y.Status == "Draft", ct);
            if (x is null || t.UserId is null || !t.CanAccessBranch(x.BranchId)) return Results.NotFound();
            x.Status = "Approved"; x.ApprovedByUserId = t.UserId; await db.SaveChangesAsync(ct); return Results.Ok(x);
        }).RequireAuthorization("expenses.approve");
        api.MapPost("/expenses/{id:guid}/mark-paid", async (Guid id, TenantContext t, AppDbContext db, CancellationToken ct) =>
        {
            var x = await db.ExpenseRecords.SingleOrDefaultAsync(y => y.Id == id && y.Status == "Approved", ct);
            if (x is null || t.UserId is null || !t.CanAccessBranch(x.BranchId)) return Results.NotFound();
            x.Status = "Paid"; x.PaidByUserId = t.UserId; x.PaidAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(x);
        }).RequireAuthorization("expenses.mark_paid");
        return endpoints;
    }
    private static IResult Result(CommercialResult r) => r.IsSuccess ? Results.Ok(r) : Results.Conflict(r);
}
