using AtiqSalon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class DepositEndpoints
{
    public static IEndpointRouteBuilder MapDepositApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapPost("/deposits", async (CreateDepositRequest request, DepositService service, CancellationToken ct) =>
        {
            var result = await service.Create(request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/deposits/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("deposits.create");
        api.MapGet("/deposits", async (Guid customerId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.CustomerDeposits.Where(x => x.CustomerId == customerId && x.AvailableAmount > 0)
                .OrderBy(x => x.CreatedAtUtc).ToListAsync(ct))).RequireAuthorization("deposits.read");
        api.MapPost("/deposits/{id:guid}/apply", async (Guid id, ApplyDepositRequest request,
            DepositService service, CancellationToken ct) =>
        {
            var result = await service.Apply(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("deposits.apply");
        return endpoints;
    }
}
