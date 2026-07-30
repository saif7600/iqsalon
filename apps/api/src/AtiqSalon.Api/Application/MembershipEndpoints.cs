using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/membership-plans", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.MembershipPlans.Where(x => x.IsActive).OrderBy(x => x.Name)
                .ToListAsync(ct))).RequireAuthorization("memberships.read");
        api.MapPost("/membership-plans", async (CreateMembershipPlanRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.RecurringPrice < 0 || request.IncludedCredits < 0
                || request.BillingInterval is not ("Weekly" or "Monthly" or "Quarterly" or "Annual"))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["plan"] = ["Valid pricing, credits, and billing interval are required."] });
            var plan = new MembershipPlan
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                RecurringPrice = CommercialRules.Round(request.RecurringPrice),
                BillingInterval = request.BillingInterval,
                IncludedCredits = request.IncludedCredits
            };
            db.MembershipPlans.Add(plan);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/membership-plans/{plan.Id}", plan);
        }).RequireAuthorization("memberships.manage");
        api.MapPost("/membership-plans/{id:guid}/enroll", async (Guid id,
            EnrollMembershipRequest request, MembershipService service, CancellationToken ct) =>
        {
            var result = await service.Enroll(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("memberships.sell");
        api.MapGet("/customer-memberships", async (Guid customerId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.CustomerMemberships.Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.StartsAtUtc).ToListAsync(ct))).RequireAuthorization("memberships.read");
        api.MapPost("/customer-memberships/{id:guid}/renew", async (Guid id,
            RenewMembershipRequest request, MembershipService service, CancellationToken ct) =>
        {
            var result = await service.Renew(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("memberships.renew");
        api.MapPost("/customer-memberships/{id:guid}/consume", async (Guid id,
            ConsumeMembershipRequest request, MembershipService service, CancellationToken ct) =>
        {
            var result = await service.Consume(id, request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("memberships.consume");
        return endpoints;
    }
}

public sealed record CreateMembershipPlanRequest(Guid OrganizationId, string Code, string Name,
    string? Description, decimal RecurringPrice, string BillingInterval, decimal IncludedCredits);
