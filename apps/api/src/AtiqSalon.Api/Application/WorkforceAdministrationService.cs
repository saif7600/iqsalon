using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class WorkforceAdministrationService(AppDbContext db, TenantContext tenant)
{
    public async Task<(bool Success, string Code, LeaveRequest? Request)> DecideLeaveAsync(
        Guid id, bool approve, string? note, CancellationToken ct)
    {
        if (tenant.UserId is null) return (false, "unauthorized", null);
        var request = await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (request is null) return (false, "not_found", null);
        if (request.Status != "Pending") return (false, "already_decided", null);
        var balance = await db.StaffLeaveBalances.SingleOrDefaultAsync(x =>
            x.StaffMemberId == request.StaffMemberId && x.LeaveTypeId == request.LeaveTypeId
            && x.Year == request.StartsOn.Year, ct);
        if (approve && (balance is null || balance.AvailableDays < request.RequestedDays))
            return (false, "insufficient_balance", null);
        request.Status = approve ? "Approved" : "Rejected";
        request.DecisionNote = note?.Trim();
        request.DecidedByUserId = tenant.UserId;
        request.DecidedAtUtc = DateTimeOffset.UtcNow;
        if (approve) balance!.UsedDays += request.RequestedDays;
        await db.SaveChangesAsync(ct);
        return (true, request.Status.ToLowerInvariant(), request);
    }

    public async Task<(bool Success, string Code)> DecideSwapAsync(
        Guid id, bool approve, string? note, CancellationToken ct)
    {
        if (tenant.UserId is null) return (false, "unauthorized");
        var swap = await db.ShiftSwapRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (swap is null) return (false, "not_found");
        if (swap.Status != "AwaitingManager" || !swap.RecipientAccepted)
            return (false, "recipient_acceptance_required");
        if (!tenant.CanAccessBranch(swap.BranchId)) return (false, "branch_forbidden");
        swap.Status = approve ? "Approved" : "Rejected";
        swap.DecidedByUserId = tenant.UserId;
        swap.DecidedAtUtc = DateTimeOffset.UtcNow;
        swap.DecisionNote = note?.Trim();
        if (approve)
        {
            var offered = await db.StaffShifts.SingleAsync(x => x.Id == swap.OfferedShiftId, ct);
            offered.StaffMemberId = swap.RequestedStaffMemberId;
            if (swap.RequestedShiftId.HasValue)
            {
                var requested = await db.StaffShifts.SingleAsync(x => x.Id == swap.RequestedShiftId, ct);
                requested.StaffMemberId = swap.OfferedByStaffMemberId;
            }
        }
        await db.SaveChangesAsync(ct);
        return (true, swap.Status.ToLowerInvariant());
    }

    public async Task<(bool Success, string Code, PayrollInputBatch? Batch)> GeneratePayrollInputsAsync(
        GeneratePayrollInputsRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null) return (false, "unauthorized", null);
        if (request.PeriodEnd < request.PeriodStart) return (false, "invalid_period", null);
        if (request.BranchId.HasValue && !tenant.CanAccessBranch(request.BranchId.Value))
            return (false, "branch_forbidden", null);
        var attendance = db.AttendanceRecords.Where(x => x.OrganizationId == request.OrganizationId
            && x.BusinessDate >= request.PeriodStart && x.BusinessDate <= request.PeriodEnd);
        if (request.BranchId.HasValue) attendance = attendance.Where(x => x.BranchId == request.BranchId);
        var approvedIds = db.AttendanceApprovals.Select(x => x.AttendanceRecordId);
        var attendanceRows = await attendance.Where(x => approvedIds.Contains(x.Id)).ToListAsync(ct);
        var leaveRows = await db.LeaveRequests.Where(x => x.OrganizationId == request.OrganizationId
            && x.Status == "Approved" && x.StartsOn <= request.PeriodEnd
            && x.EndsOn >= request.PeriodStart).ToListAsync(ct);
        var leaveTypes = await db.LeaveTypes.ToDictionaryAsync(x => x.Id, ct);
        var commissions = db.CommissionLedgerEntries.Where(x => x.OrganizationId == request.OrganizationId
            && x.BusinessDate >= request.PeriodStart && x.BusinessDate <= request.PeriodEnd);
        if (request.BranchId.HasValue) commissions = commissions.Where(x => x.BranchId == request.BranchId);
        var commissionRows = await commissions.ToListAsync(ct);
        var staffIds = attendanceRows.Select(x => x.StaffMemberId)
            .Concat(leaveRows.Select(x => x.StaffMemberId))
            .Concat(commissionRows.Select(x => x.StaffMemberId)).Distinct().ToList();
        var batch = new PayrollInputBatch
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            CreatedByUserId = tenant.UserId.Value
        };
        db.PayrollInputBatches.Add(batch);
        foreach (var staffId in staffIds)
        {
            var staffAttendance = attendanceRows.Where(x => x.StaffMemberId == staffId);
            var staffLeave = leaveRows.Where(x => x.StaffMemberId == staffId);
            db.PayrollInputLines.Add(new PayrollInputLine
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                PayrollInputBatchId = batch.Id,
                StaffMemberId = staffId,
                WorkedMinutes = staffAttendance.Sum(x => x.WorkedMinutes),
                OvertimeMinutes = staffAttendance.Sum(x => x.OvertimeMinutes),
                PaidLeaveDays = staffLeave.Where(x => leaveTypes.GetValueOrDefault(x.LeaveTypeId)?.IsPaid == true)
                    .Sum(x => x.RequestedDays),
                UnpaidLeaveDays = staffLeave.Where(x => leaveTypes.GetValueOrDefault(x.LeaveTypeId)?.IsPaid != true)
                    .Sum(x => x.RequestedDays),
                CommissionAmount = commissionRows.Where(x => x.StaffMemberId == staffId).Sum(x => x.Amount)
            });
        }
        await db.SaveChangesAsync(ct);
        return (true, "generated", batch);
    }
}

public sealed record GeneratePayrollInputsRequest(Guid OrganizationId, Guid? BranchId,
    DateOnly PeriodStart, DateOnly PeriodEnd);
