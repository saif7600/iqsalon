using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class CommissionEndpoints
{
    public static IEndpointRouteBuilder MapCommissionApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/commission-plans", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.CommissionPlans.OrderBy(x => x.Name).ToListAsync(ct)))
            .RequireAuthorization("commissions.read");
        api.MapPost("/commission-plans", async (CreateCommissionPlanRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.Basis is not ("NetRevenue" or "GrossRevenue" or "GrossProfit")
                || request.ServiceRatePercentage is < 0 or > 100 || request.ProductRatePercentage is < 0 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["plan"] = ["Valid basis and rates between 0 and 100 are required."] });
            var plan = new CommissionPlan
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Basis = request.Basis,
                ServiceRatePercentage = request.ServiceRatePercentage,
                ProductRatePercentage = request.ProductRatePercentage,
                IncludeTips = request.IncludeTips
            };
            db.CommissionPlans.Add(plan);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/commission-plans/{plan.Id}", plan);
        }).RequireAuthorization("commissions.manage");
        api.MapPost("/commission-assignments", async (CreateCommissionAssignmentRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || !tenant.CanAccessBranch(request.BranchId))
                return Results.Forbid();
            if (!await db.StaffMembers.AnyAsync(x => x.Id == request.StaffMemberId
                    && x.OrganizationId == request.OrganizationId, ct)
                || !await db.CommissionPlans.AnyAsync(x => x.Id == request.CommissionPlanId
                    && x.OrganizationId == request.OrganizationId && x.IsActive, ct))
                return Results.NotFound();
            var assignment = new StaffCommissionAssignment
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                BranchId = request.BranchId,
                StaffMemberId = request.StaffMemberId,
                CommissionPlanId = request.CommissionPlanId,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo
            };
            db.StaffCommissionAssignments.Add(assignment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/commission-assignments/{assignment.Id}", assignment);
        }).RequireAuthorization("commissions.manage");
        api.MapGet("/commission-ledger", async (Guid? branchId, Guid? staffMemberId,
            DateOnly? from, DateOnly? to, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid();
            var query = db.CommissionLedgerEntries.AsQueryable();
            if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId));
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            if (staffMemberId.HasValue) query = query.Where(x => x.StaffMemberId == staffMemberId);
            if (from.HasValue) query = query.Where(x => x.BusinessDate >= from);
            if (to.HasValue) query = query.Where(x => x.BusinessDate <= to);
            return Results.Ok(await query.OrderByDescending(x => x.OccurredAtUtc).Take(1000).ToListAsync(ct));
        }).RequireAuthorization("commissions.read");
        return endpoints;
    }
}

public sealed record CreateCommissionPlanRequest(Guid OrganizationId, string Code, string Name,
    string Basis, decimal ServiceRatePercentage, decimal ProductRatePercentage, bool IncludeTips = false);
public sealed record CreateCommissionAssignmentRequest(Guid OrganizationId, Guid BranchId,
    Guid StaffMemberId, Guid CommissionPlanId, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
