using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/stock-transfers", async (Guid branchId, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
            !tenant.CanAccessBranch(branchId)
                ? Results.Forbid()
                : Results.Ok(await db.StockTransfers
                    .Where(x => x.SourceBranchId == branchId || x.DestinationBranchId == branchId)
                    .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)))
            .RequireAuthorization("stock_transfers.read");
        api.MapPost("/stock-transfers", async (CreateTransferRequest request, TransferService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/stock-transfers/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("stock_transfers.create");
        MapAction(api, "approve", "stock_transfers.approve");
        MapAction(api, "dispatch", "stock_transfers.dispatch");
        MapAction(api, "receive", "stock_transfers.receive");
        return endpoints;
    }

    private static void MapAction(RouteGroupBuilder api, string action, string permission) =>
        api.MapPost($"/stock-transfers/{{id:guid}}/{action}",
            async (Guid id, TransferService service, CancellationToken ct) =>
            {
                var result = await service.Transition(id, action, ct);
                return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
            }).RequireAuthorization(permission);
}
