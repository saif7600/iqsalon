using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class DailyClosingEndpoints
{
    public static IEndpointRouteBuilder MapDailyClosingApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapPost("/daily-closings", async (CreateDailyClosingRequest request,
            DailyClosingService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/daily-closings/{result.Id}", result)
                : Results.Conflict(result);
        }).RequireAuthorization("daily_closing.create");
        api.MapGet("/daily-closings", async (Guid? branchId, DateOnly? from, DateOnly? to,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid();
            var query = db.BranchDailyClosings.AsQueryable();
            if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId));
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            if (from.HasValue) query = query.Where(x => x.BusinessDate >= from);
            if (to.HasValue) query = query.Where(x => x.BusinessDate <= to);
            return Results.Ok(await query.OrderByDescending(x => x.BusinessDate).Take(400).ToListAsync(ct));
        }).RequireAuthorization("daily_closing.read");
        api.MapGet("/daily-closings/{id:guid}", async (Guid id, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
            await db.BranchDailyClosings.SingleOrDefaultAsync(x => x.Id == id, ct) is { } closing
                && tenant.CanAccessBranch(closing.BranchId)
                ? Results.Ok(closing)
                : Results.NotFound()).RequireAuthorization("daily_closing.read");
        api.MapPost("/daily-closings/{id:guid}/approve", async (Guid id,
            ApproveDailyClosingRequest request, DailyClosingService service, CancellationToken ct) =>
        {
            var result = await service.Approve(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("daily_closing.approve");
        return endpoints;
    }
}
