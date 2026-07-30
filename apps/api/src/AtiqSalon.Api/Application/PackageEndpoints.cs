using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class PackageEndpoints
{
    public static IEndpointRouteBuilder MapPackageApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/packages", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.PackageDefinitions.Where(x => x.IsActive).OrderBy(x => x.Name)
                .ToListAsync(ct))).RequireAuthorization("packages.read");
        api.MapPost("/packages", async (CreatePackageRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.Price < 0 || request.ValidityDays <= 0
                || request.Entitlements.Count == 0 || request.Entitlements.Any(x => x.Quantity <= 0))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["package"] = ["Valid price, validity, and entitlements are required."] });
            var serviceIds = request.Entitlements.Select(x => x.ServiceId).Distinct().ToArray();
            if (await db.SalonServices.CountAsync(x => serviceIds.Contains(x.Id)
                && x.OrganizationId == request.OrganizationId && x.IsActive, ct) != serviceIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["entitlements"] = ["Every service must be active in this organization."] });
            var definition = new PackageDefinition
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Price = CommercialRules.Round(request.Price),
                ValidityDays = request.ValidityDays
            };
            db.PackageDefinitions.Add(definition);
            db.PackageEntitlements.AddRange(request.Entitlements.Select(x => new PackageEntitlement
            {
                TenantId = definition.TenantId,
                OrganizationId = definition.OrganizationId,
                PackageDefinitionId = definition.Id,
                ServiceId = x.ServiceId,
                Quantity = x.Quantity
            }));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/packages/{definition.Id}", definition);
        }).RequireAuthorization("packages.manage");
        api.MapPost("/packages/{id:guid}/activate", async (Guid id, ActivatePackageRequest request,
            PackageService service, CancellationToken ct) =>
        {
            var result = await service.Activate(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("packages.sell");
        api.MapGet("/customer-packages", async (Guid customerId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.CustomerPackages.Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.PurchasedAtUtc).ToListAsync(ct))).RequireAuthorization("packages.read");
        api.MapPost("/customer-packages/{id:guid}/consume", async (Guid id, ConsumePackageRequest request,
            PackageService service, CancellationToken ct) =>
        {
            var result = await service.Consume(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("packages.consume");
        return endpoints;
    }
}

public sealed record PackageEntitlementRequest(Guid ServiceId, decimal Quantity);
public sealed record CreatePackageRequest(Guid OrganizationId, string Code, string Name,
    string? Description, decimal Price, int ValidityDays, IReadOnlyList<PackageEntitlementRequest> Entitlements);
