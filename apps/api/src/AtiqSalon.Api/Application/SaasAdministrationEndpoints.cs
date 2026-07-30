namespace AtiqSalon.Api.Application;

public static class SaasAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapSaasAdministrationApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform").WithTags("Platform administration");
        group.MapGet("/overview", (SaasAdministrationService service, CancellationToken ct) => service.GetOverviewAsync(ct)).RequireAuthorization("platform.dashboard.read");
        group.MapGet("/tenants", (SaasAdministrationService service, CancellationToken ct) => service.GetTenantsAsync(ct)).RequireAuthorization("platform.tenants.read");
        group.MapPost("/tenants", async (ProvisionTenantRequest request, SaasAdministrationService service, CancellationToken ct) =>
        {
            var (result, error) = await service.ProvisionTenantAsync(request, ct);
            return error is null ? Results.Created("/api/v1/platform/tenants", result) : Results.BadRequest(new { error });
        }).RequireAuthorization("platform.tenants.manage");
        group.MapGet("/plans", (SaasAdministrationService service, CancellationToken ct) => service.GetPlansAsync(ct)).RequireAuthorization("platform.plans.read");
        group.MapPost("/plans", async (CreateSaasPlanRequest request, SaasAdministrationService service, CancellationToken ct) =>
        {
            var (result, error) = await service.CreatePlanAsync(request, ct);
            return error is null ? Results.Created("/api/v1/platform/plans", result) : Results.BadRequest(new { error });
        }).RequireAuthorization("platform.plans.manage");
        group.MapGet("/subscriptions", (SaasAdministrationService service, CancellationToken ct) => service.GetSubscriptionsAsync(ct)).RequireAuthorization("platform.subscriptions.read");
        group.MapPost("/subscriptions", async (ActivateSaasSubscriptionRequest request, SaasAdministrationService service, CancellationToken ct) =>
        {
            var (result, error) = await service.ActivateSubscriptionAsync(request, ct);
            return error is null ? Results.Created("/api/v1/platform/subscriptions", result) : Results.BadRequest(new { error });
        }).RequireAuthorization("platform.subscriptions.manage");
        return app;
    }
}
