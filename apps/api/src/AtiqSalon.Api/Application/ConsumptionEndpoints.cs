using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class ConsumptionEndpoints
{
    public static IEndpointRouteBuilder MapConsumptionApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/service-recipes", async (Guid serviceId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ServiceRecipes.Where(x => x.ServiceId == serviceId)
                .OrderByDescending(x => x.VersionNumber).ToListAsync(ct))).RequireAuthorization("recipes.read");
        api.MapPost("/service-recipes", async (CreateRecipeRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || request.Lines.Count == 0
                || request.Lines.Any(x => x.QuantityBaseUnit <= 0 || x.WastageAllowancePercent is < 0 or > 100))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["recipe"] = ["Valid recipe lines are required."] });
            if (!await db.SalonServices.AnyAsync(x => x.Id == request.ServiceId && x.OrganizationId == request.OrganizationId, ct))
                return Results.NotFound();
            var version = await db.ServiceRecipes.Where(x => x.ServiceId == request.ServiceId)
                .Select(x => (int?)x.VersionNumber).MaxAsync(ct) ?? 0;
            var recipe = new ServiceRecipe
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                ServiceId = request.ServiceId,
                VersionNumber = version + 1,
                Name = request.Name.Trim(),
                CreatedByUserId = tenant.UserId.Value
            };
            db.ServiceRecipes.Add(recipe);
            db.ServiceRecipeLines.AddRange(request.Lines.Select((x, index) => new ServiceRecipeLine
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                ServiceRecipeId = recipe.Id,
                ProductId = x.ProductId,
                QuantityBaseUnit = x.QuantityBaseUnit,
                WastageAllowancePercent = x.WastageAllowancePercent,
                Sequence = index + 1
            }));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/service-recipes/{recipe.Id}", recipe);
        }).RequireAuthorization("recipes.create");
        api.MapPost("/service-recipes/{id:guid}/activate", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var recipe = await db.ServiceRecipes.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (recipe is null || tenant.UserId is null) return Results.NotFound();
            var active = await db.ServiceRecipes.Where(x => x.ServiceId == recipe.ServiceId && x.Status == "Active").ToListAsync(ct);
            foreach (var prior in active) { prior.Status = "Superseded"; prior.EffectiveToUtc = DateTimeOffset.UtcNow; }
            recipe.Status = "Active"; recipe.EffectiveFromUtc = DateTimeOffset.UtcNow; recipe.ActivatedByUserId = tenant.UserId;
            await db.SaveChangesAsync(ct); return Results.Ok(recipe);
        }).RequireAuthorization("recipes.activate");
        api.MapGet("/appointments/{appointmentId:guid}/consumption", async (Guid appointmentId, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.AppointmentConsumptions.SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return item is null ? Results.NotFound() : Results.Ok(new
            {
                consumption = item,
                lines = await db.AppointmentConsumptionLines.Where(x => x.AppointmentConsumptionId == item.Id).ToListAsync(ct)
            });
        }).RequireAuthorization("consumption.read");
        api.MapPost("/appointments/{appointmentId:guid}/consumption", async (Guid appointmentId, ConsumptionService service, CancellationToken ct) =>
        {
            var result = await service.ConsumeAppointment(appointmentId, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("consumption.confirm");
        api.MapPost("/consumptions/{id:guid}/reverse", async (Guid id, ReverseConsumptionRequest request,
            ConsumptionService service, CancellationToken ct) =>
        {
            var result = await service.Reverse(id, request.Reason, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("consumption.reverse");
        return endpoints;
    }
}

public sealed record RecipeLineRequest(Guid ProductId, decimal QuantityBaseUnit, decimal WastageAllowancePercent);
public sealed record CreateRecipeRequest(Guid OrganizationId, Guid ServiceId, string Name, IReadOnlyCollection<RecipeLineRequest> Lines);
public sealed record ReverseConsumptionRequest(string Reason);
