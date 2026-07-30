using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class PerformanceAndLoyaltyService(AppDbContext db, TenantContext tenant)
{
    public async Task<(bool Success, string Code, LoyaltyLedgerEntry? Entry)> PostPointsAsync(
        LoyaltyPointsRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null) return (false, "unauthorized", null);
        if (request.Points == 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return (false, "invalid_entry", null);
        var prior = await db.LoyaltyLedgerEntries.SingleOrDefaultAsync(
            x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (prior is not null) return (true, "duplicate", prior);
        var account = await db.CustomerLoyaltyAccounts.SingleOrDefaultAsync(
            x => x.Id == request.AccountId, ct);
        if (account is null) return (false, "not_found", null);
        if (account.PointsBalance + request.Points < 0) return (false, "insufficient_points", null);
        var entry = new LoyaltyLedgerEntry
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = account.OrganizationId,
            CustomerLoyaltyAccountId = account.Id,
            SaleId = request.SaleId,
            EntryType = request.Points > 0 ? "Earn" : "Redeem",
            Points = request.Points,
            Reason = request.Reason.Trim(),
            IdempotencyKey = request.IdempotencyKey.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        account.PointsBalance += request.Points;
        if (request.Points > 0) account.LifetimePoints += request.Points;
        var tier = await db.LoyaltyTiers.Where(x => x.LoyaltyProgramId == account.LoyaltyProgramId
                && x.MinimumLifetimePoints <= account.LifetimePoints)
            .OrderByDescending(x => x.MinimumLifetimePoints).FirstOrDefaultAsync(ct);
        account.LoyaltyTierId = tier?.Id;
        db.LoyaltyLedgerEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return (true, "posted", entry);
    }
}

public sealed record LoyaltyPointsRequest(Guid AccountId, Guid? SaleId, decimal Points,
    string Reason, string IdempotencyKey);

public static class PerformanceAndLoyaltyEndpoints
{
    private static readonly string[] TargetMetrics =
        ["ServiceRevenue", "ProductRevenue", "BookingsCompleted", "RebookingRate", "RetailAttachRate"];

    public static IEndpointRouteBuilder MapPerformanceAndLoyaltyApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/performance/targets", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.PerformanceTargets.OrderByDescending(x => x.PeriodStart).ToListAsync(ct)))
            .RequireAuthorization("performance.read");
        api.MapPost("/performance/targets", async (PerformanceTargetRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || request.PeriodEnd < request.PeriodStart
                || request.TargetValue < 0 || !TargetMetrics.Contains(request.Metric))
                return Results.BadRequest();
            if (request.BranchId.HasValue && !tenant.CanAccessBranch(request.BranchId.Value))
                return Results.Forbid();
            var item = new PerformanceTarget
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                BranchId = request.BranchId,
                StaffMemberId = request.StaffMemberId,
                Metric = request.Metric,
                TargetValue = request.TargetValue,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                CreatedByUserId = tenant.UserId.Value
            };
            db.PerformanceTargets.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/performance/targets/{item.Id}", item);
        }).RequireAuthorization("performance.manage");
        api.MapPost("/performance/reviews", async (PerformanceReviewRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || request.Rating is < 1 or > 5
                || request.PeriodEnd < request.PeriodStart) return Results.BadRequest();
            var item = new PerformanceReview
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                StaffMemberId = request.StaffMemberId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Rating = request.Rating,
                Summary = request.Summary.Trim(),
                ReviewerUserId = tenant.UserId.Value
            };
            db.PerformanceReviews.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/performance/reviews/{item.Id}", item);
        }).RequireAuthorization("performance.manage");
        api.MapGet("/loyalty/accounts/{customerId:guid}", async (Guid customerId,
            AppDbContext db, CancellationToken ct) => Results.Ok(await db.CustomerLoyaltyAccounts
                .SingleOrDefaultAsync(x => x.CustomerId == customerId, ct)))
            .RequireAuthorization("loyalty.read");
        api.MapPost("/loyalty/points", async (LoyaltyPointsRequest request,
            PerformanceAndLoyaltyService service, CancellationToken ct) =>
        {
            var result = await service.PostPointsAsync(request, ct);
            return result.Success ? Results.Ok(result.Entry) : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("loyalty.adjust");
        api.MapPost("/referrals", async (ReferralRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.ReferrerCustomerId == request.ReferredCustomerId)
                return Results.BadRequest();
            var code = await db.ReferralCodes.SingleOrDefaultAsync(
                x => x.Code == request.Code.Trim().ToUpperInvariant() && x.IsActive, ct);
            if (code is null || code.CustomerId != request.ReferrerCustomerId) return Results.BadRequest();
            if (await db.CustomerReferrals.AnyAsync(x => x.ReferredCustomerId == request.ReferredCustomerId, ct))
                return Results.Conflict();
            var item = new CustomerReferral
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = code.OrganizationId,
                ReferralCodeId = code.Id,
                ReferrerCustomerId = request.ReferrerCustomerId,
                ReferredCustomerId = request.ReferredCustomerId
            };
            db.CustomerReferrals.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/referrals/{item.Id}", item);
        }).RequireAuthorization("referrals.manage");
        return endpoints;
    }
}

public sealed record PerformanceTargetRequest(Guid OrganizationId, Guid? BranchId,
    Guid? StaffMemberId, string Metric, decimal TargetValue, DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record PerformanceReviewRequest(Guid OrganizationId, Guid StaffMemberId,
    DateOnly PeriodStart, DateOnly PeriodEnd, decimal Rating, string Summary);
public sealed record ReferralRequest(string Code, Guid ReferrerCustomerId, Guid ReferredCustomerId);
