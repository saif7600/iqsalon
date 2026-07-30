using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class WorkforceEndpoints
{
    public static IEndpointRouteBuilder MapWorkforceApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/workforce");
        api.MapGet("/shift-templates", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ShiftTemplates.OrderBy(x => x.Name).ToListAsync(ct)))
            .RequireAuthorization("shifts.read");
        api.MapPost("/shift-templates", async (ShiftTemplateRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.EndTime <= request.StartTime || request.UnpaidBreakMinutes < 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["template"] = ["A valid same-day shift is required."] });
            var item = new ShiftTemplate
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                UnpaidBreakMinutes = request.UnpaidBreakMinutes
            };
            db.ShiftTemplates.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/workforce/shift-templates/{item.Id}", item);
        }).RequireAuthorization("shifts.manage");
        api.MapGet("/shifts", async (Guid? branchId, DateTimeOffset? from, DateTimeOffset? to,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid();
            var query = db.StaffShifts.AsQueryable();
            if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId));
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            if (from.HasValue) query = query.Where(x => x.EndsAtUtc >= from);
            if (to.HasValue) query = query.Where(x => x.StartsAtUtc <= to);
            return Results.Ok(await query.OrderBy(x => x.StartsAtUtc).Take(2000).ToListAsync(ct));
        }).RequireAuthorization("shifts.read");
        api.MapPost("/shifts", async (StaffShiftRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
                return Results.Forbid();
            if (request.EndsAtUtc <= request.StartsAtUtc || !await db.StaffBranchAssignments.AnyAsync(
                    x => x.StaffMemberId == request.StaffMemberId && x.BranchId == request.BranchId && x.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["shift"] = ["A valid shift for assigned staff is required."] });
            var item = new StaffShift
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                BranchId = request.BranchId,
                StaffMemberId = request.StaffMemberId,
                ShiftTemplateId = request.ShiftTemplateId,
                StartsAtUtc = request.StartsAtUtc,
                EndsAtUtc = request.EndsAtUtc,
                UnpaidBreakMinutes = request.UnpaidBreakMinutes,
                Notes = request.Notes?.Trim(),
                CreatedByUserId = tenant.UserId.Value
            };
            db.StaffShifts.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/workforce/shifts/{item.Id}", item);
        }).RequireAuthorization("shifts.manage");
        api.MapGet("/attendance", async (Guid? branchId, TenantContext tenant, AppDbContext db,
            CancellationToken ct) =>
        {
            if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid();
            var query = db.AttendanceRecords.AsQueryable();
            if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId));
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            return Results.Ok(await query.OrderByDescending(x => x.BusinessDate).Take(2000).ToListAsync(ct));
        }).RequireAuthorization("attendance.read");
        api.MapPost("/attendance/events", async (RecordAttendanceRequest request,
            WorkforceService service, CancellationToken ct) =>
        {
            var result = await service.RecordAsync(request, ct);
            return result.Success ? Results.Ok(result.Event) : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("attendance.record");
        api.MapPost("/attendance/events/{id:guid}/corrections", async (Guid id,
            CorrectAttendanceRequest request, WorkforceService service, CancellationToken ct) =>
        {
            var result = await service.CorrectAsync(id, request, ct);
            return result.Success ? Results.Ok(result.Event) : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("attendance.correct");
        return endpoints;
    }
}

public sealed record ShiftTemplateRequest(Guid OrganizationId, string Code, string Name,
    TimeOnly StartTime, TimeOnly EndTime, int UnpaidBreakMinutes);
public sealed record StaffShiftRequest(Guid OrganizationId, Guid BranchId, Guid StaffMemberId,
    Guid? ShiftTemplateId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    int UnpaidBreakMinutes, string? Notes);
