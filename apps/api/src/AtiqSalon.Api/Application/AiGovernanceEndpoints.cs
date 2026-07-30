using AtiqSalon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class AiGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapAiGovernanceApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/ai");
        api.MapGet("/settings", async (Guid organizationId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TenantAiSettings.SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId, ct)))
            .RequireAuthorization("ai.settings.read");
        api.MapPut("/settings", async (Guid organizationId, UpdateAiSettingsRequest request,
            AiGovernanceService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.UpdateSettingsAsync(organizationId, request, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization("ai.settings.update");
        api.MapGet("/models", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.AiModelDefinitions.Where(x => x.IsEnabled)
                .OrderBy(x => x.Provider).ThenBy(x => x.DisplayName).ToListAsync(ct)))
            .RequireAuthorization("ai.settings.read");
        api.MapGet("/usage", async (Guid organizationId, DateOnly? from, DateOnly? to,
            AppDbContext db, CancellationToken ct) =>
        {
            var query = db.AiUsageEntries.Where(x => x.OrganizationId == organizationId);
            if (from.HasValue) query = query.Where(x => x.UsageDate >= from);
            if (to.HasValue) query = query.Where(x => x.UsageDate <= to);
            return Results.Ok(await query.GroupBy(x => new { x.UsageDate, x.Provider, x.Model })
                .Select(x => new
                {
                    x.Key.UsageDate,
                    x.Key.Provider,
                    x.Key.Model,
                    Requests = x.Count(),
                    InputTokens = x.Sum(y => y.InputTokens),
                    OutputTokens = x.Sum(y => y.OutputTokens),
                    EstimatedCost = x.Sum(y => y.EstimatedCost)
                })
                .OrderByDescending(x => x.UsageDate).ToListAsync(ct));
        }).RequireAuthorization("ai.usage.read");
        return endpoints;
    }
}
