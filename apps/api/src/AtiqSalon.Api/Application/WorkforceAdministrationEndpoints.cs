using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class WorkforceAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapWorkforceAdministrationApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/workforce");
        api.MapGet("/leave-types", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LeaveTypes.OrderBy(x => x.Name).ToListAsync(ct)))
            .RequireAuthorization("leave.read");
        api.MapPost("/leave-types", async (LeaveTypeRequest request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || request.DefaultAnnualDays < 0)
                return Results.BadRequest();
            var item = new LeaveType
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                IsPaid = request.IsPaid,
                DefaultAnnualDays = request.DefaultAnnualDays
            };
            db.LeaveTypes.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/workforce/leave-types/{item.Id}", item);
        }).RequireAuthorization("leave.manage");
        api.MapGet("/leave-requests", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LeaveRequests.OrderByDescending(x => x.RequestedAtUtc).Take(1000).ToListAsync(ct)))
            .RequireAuthorization("leave.read");
        api.MapPost("/leave-requests", async (LeaveRequestInput request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || request.EndsOn < request.StartsOn
                || request.RequestedDays <= 0) return Results.BadRequest();
            var item = new LeaveRequest
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                StaffMemberId = request.StaffMemberId,
                LeaveTypeId = request.LeaveTypeId,
                StartsOn = request.StartsOn,
                EndsOn = request.EndsOn,
                RequestedDays = request.RequestedDays,
                StaffNote = request.Note?.Trim(),
                RequestedByUserId = tenant.UserId.Value
            };
            db.LeaveRequests.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/workforce/leave-requests/{item.Id}", item);
        }).RequireAuthorization("leave.request");
        api.MapPost("/leave-requests/{id:guid}/decision", async (Guid id, DecisionRequest request,
            WorkforceAdministrationService service, CancellationToken ct) =>
        {
            var result = await service.DecideLeaveAsync(id, request.Approve, request.Note, ct);
            return result.Success ? Results.Ok(result.Request) : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("leave.approve");
        api.MapPost("/attendance/{id:guid}/approve", async (Guid id, ApprovalRequest request,
            TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null) return Results.Unauthorized();
            var record = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (record is null) return Results.NotFound();
            if (!tenant.CanAccessBranch(record.BranchId)) return Results.Forbid();
            if (!await db.AttendanceApprovals.AnyAsync(x => x.AttendanceRecordId == id, ct))
                db.AttendanceApprovals.Add(new AttendanceApproval
                {
                    TenantId = tenant.TenantId.Value,
                    OrganizationId = record.OrganizationId,
                    BranchId = record.BranchId,
                    AttendanceRecordId = id,
                    ApprovedByUserId = tenant.UserId.Value,
                    Note = request.Note?.Trim()
                });
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("attendance.approve");
        api.MapPost("/shift-swaps", async (ShiftSwapInput request, TenantContext tenant,
            AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
                return Results.Forbid();
            var item = new ShiftSwapRequest
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                BranchId = request.BranchId,
                OfferedShiftId = request.OfferedShiftId,
                OfferedByStaffMemberId = request.OfferedByStaffMemberId,
                RequestedStaffMemberId = request.RequestedStaffMemberId,
                RequestedShiftId = request.RequestedShiftId,
                Reason = request.Reason?.Trim(),
                RequestedByUserId = tenant.UserId.Value
            };
            db.ShiftSwapRequests.Add(item); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/workforce/shift-swaps/{item.Id}", item);
        }).RequireAuthorization("shift_swaps.request");
        api.MapPost("/shift-swaps/{id:guid}/accept", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.ShiftSwapRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null || item.Status != "AwaitingRecipient") return Results.BadRequest();
            item.RecipientAccepted = true; item.Status = "AwaitingManager";
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("shift_swaps.accept");
        api.MapPost("/shift-swaps/{id:guid}/decision", async (Guid id, DecisionRequest request,
            WorkforceAdministrationService service, CancellationToken ct) =>
        {
            var result = await service.DecideSwapAsync(id, request.Approve, request.Note, ct);
            return result.Success ? Results.NoContent() : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("shift_swaps.approve");
        api.MapPost("/payroll-inputs", async (GeneratePayrollInputsRequest request,
            WorkforceAdministrationService service, CancellationToken ct) =>
        {
            var result = await service.GeneratePayrollInputsAsync(request, ct);
            return result.Success ? Results.Created($"/api/v1/workforce/payroll-inputs/{result.Batch!.Id}",
                result.Batch) : Results.BadRequest(new { code = result.Code });
        }).RequireAuthorization("payroll_inputs.manage");
        api.MapGet("/payroll-inputs/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var batch = await db.PayrollInputBatches.SingleOrDefaultAsync(x => x.Id == id, ct);
            return batch is null ? Results.NotFound() : Results.Ok(new
            {
                batch,
                lines = await db.PayrollInputLines.Where(x => x.PayrollInputBatchId == id).ToListAsync(ct)
            });
        }).RequireAuthorization("payroll_inputs.read");
        return endpoints;
    }
}

public sealed record LeaveTypeRequest(Guid OrganizationId, string Code, string Name,
    bool IsPaid, decimal DefaultAnnualDays);
public sealed record LeaveRequestInput(Guid OrganizationId, Guid StaffMemberId, Guid LeaveTypeId,
    DateOnly StartsOn, DateOnly EndsOn, decimal RequestedDays, string? Note);
public sealed record DecisionRequest(bool Approve, string? Note);
public sealed record ApprovalRequest(string? Note);
public sealed record ShiftSwapInput(Guid OrganizationId, Guid BranchId, Guid OfferedShiftId,
    Guid OfferedByStaffMemberId, Guid RequestedStaffMemberId, Guid? RequestedShiftId, string? Reason);
