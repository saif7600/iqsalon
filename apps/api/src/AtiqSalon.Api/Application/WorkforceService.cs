using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class WorkforceService(AppDbContext db, TenantContext tenant)
{
    private static readonly string[] EventTypes = ["ClockIn", "ClockOut", "BreakStart", "BreakEnd"];

    public async Task<(bool Success, string Code, AttendanceEvent? Event)> RecordAsync(
        RecordAttendanceRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null) return (false, "unauthorized", null);
        if (!tenant.CanAccessBranch(request.BranchId)) return (false, "branch_forbidden", null);
        if (!EventTypes.Contains(request.EventType)) return (false, "invalid_event_type", null);
        if (request.OccurredAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)) return (false, "future_event", null);
        if (!await db.StaffBranchAssignments.AnyAsync(x => x.StaffMemberId == request.StaffMemberId
                && x.BranchId == request.BranchId && x.IsActive, ct))
            return (false, "staff_not_assigned", null);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var duplicate = await db.AttendanceEvents.SingleOrDefaultAsync(
                x => x.IdempotencyKey == request.IdempotencyKey, ct);
            if (duplicate is not null) return (true, "duplicate", duplicate);
        }
        var item = new AttendanceEvent
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            StaffMemberId = request.StaffMemberId,
            StaffShiftId = request.StaffShiftId,
            EventType = request.EventType,
            OccurredAtUtc = request.OccurredAtUtc,
            RecordedByUserId = tenant.UserId.Value,
            Source = request.Source.Trim(),
            IdempotencyKey = request.IdempotencyKey?.Trim()
        };
        db.AttendanceEvents.Add(item);
        await RecalculateAsync(item, ct);
        await db.SaveChangesAsync(ct);
        return (true, "recorded", item);
    }

    public async Task<(bool Success, string Code, AttendanceEvent? Event)> CorrectAsync(
        Guid eventId, CorrectAttendanceRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null) return (false, "unauthorized", null);
        var original = await db.AttendanceEvents.SingleOrDefaultAsync(x => x.Id == eventId, ct);
        if (original is null) return (false, "not_found", null);
        if (!tenant.CanAccessBranch(original.BranchId)) return (false, "branch_forbidden", null);
        if (string.IsNullOrWhiteSpace(request.Reason)) return (false, "reason_required", null);
        var correction = new AttendanceEvent
        {
            TenantId = original.TenantId,
            OrganizationId = original.OrganizationId,
            BranchId = original.BranchId,
            StaffMemberId = original.StaffMemberId,
            StaffShiftId = original.StaffShiftId,
            CorrectsEventId = original.Id,
            EventType = original.EventType,
            OccurredAtUtc = request.CorrectedOccurredAtUtc,
            RecordedByUserId = tenant.UserId.Value,
            Source = "Correction",
            Reason = request.Reason.Trim()
        };
        db.AttendanceEvents.Add(correction);
        await RecalculateAsync(correction, ct);
        await db.SaveChangesAsync(ct);
        return (true, "corrected", correction);
    }

    private async Task RecalculateAsync(AttendanceEvent current, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(current.OccurredAtUtc.UtcDateTime);
        var from = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var events = await db.AttendanceEvents.Where(x => x.StaffMemberId == current.StaffMemberId
            && x.BranchId == current.BranchId && x.OccurredAtUtc >= from
            && x.OccurredAtUtc < from.AddDays(1)).ToListAsync(ct);
        events.Add(current);
        var effective = events.GroupBy(x => x.CorrectsEventId ?? x.Id)
            .Select(x => x.OrderByDescending(e => e.RecordedAtUtc).First())
            .OrderBy(x => x.OccurredAtUtc).ToList();
        var clockIn = effective.FirstOrDefault(x => x.EventType == "ClockIn")?.OccurredAtUtc;
        var clockOut = effective.LastOrDefault(x => x.EventType == "ClockOut")?.OccurredAtUtc;
        var breaks = WorkforceRules.BreakMinutes(effective);
        var worked = WorkforceRules.WorkedMinutes(clockIn, clockOut, breaks);
        var shift = current.StaffShiftId.HasValue
            ? await db.StaffShifts.SingleOrDefaultAsync(x => x.Id == current.StaffShiftId, ct) : null;
        var settings = await db.WorkforceSettings.SingleOrDefaultAsync(
            x => x.OrganizationId == current.OrganizationId, ct);
        var record = await db.AttendanceRecords.SingleOrDefaultAsync(x =>
            x.StaffMemberId == current.StaffMemberId && x.BranchId == current.BranchId
            && x.BusinessDate == date, ct);
        if (record is null)
        {
            record = new AttendanceRecord
            {
                TenantId = current.TenantId,
                OrganizationId = current.OrganizationId,
                BranchId = current.BranchId,
                StaffMemberId = current.StaffMemberId,
                StaffShiftId = current.StaffShiftId,
                BusinessDate = date
            };
            db.AttendanceRecords.Add(record);
        }
        record.ClockInAtUtc = clockIn; record.ClockOutAtUtc = clockOut;
        record.BreakMinutes = breaks; record.WorkedMinutes = worked;
        record.LateMinutes = WorkforceRules.LateMinutes(clockIn, shift?.StartsAtUtc, settings?.GraceMinutes ?? 5);
        record.OvertimeMinutes = WorkforceRules.OvertimeMinutes(worked, shift);
        record.Status = clockIn is null ? "Absent" : clockOut is null ? "Open" : "Complete";
        record.RecalculatedAtUtc = DateTimeOffset.UtcNow;
    }
}

public static class WorkforceRules
{
    public static int WorkedMinutes(DateTimeOffset? clockIn, DateTimeOffset? clockOut, int breaks) =>
        clockIn.HasValue && clockOut > clockIn
            ? Math.Max(0, (int)(clockOut.Value - clockIn.Value).TotalMinutes - Math.Max(0, breaks)) : 0;

    public static int BreakMinutes(IEnumerable<AttendanceEvent> events)
    {
        var total = 0; DateTimeOffset? started = null;
        foreach (var item in events.OrderBy(x => x.OccurredAtUtc))
        {
            if (item.EventType == "BreakStart") started = item.OccurredAtUtc;
            else if (item.EventType == "BreakEnd" && started.HasValue && item.OccurredAtUtc > started)
            { total += (int)(item.OccurredAtUtc - started.Value).TotalMinutes; started = null; }
        }
        return total;
    }

    public static int LateMinutes(DateTimeOffset? clockIn, DateTimeOffset? shiftStart, int grace) =>
        clockIn.HasValue && shiftStart.HasValue
            ? Math.Max(0, (int)(clockIn.Value - shiftStart.Value).TotalMinutes - Math.Max(0, grace)) : 0;

    public static int OvertimeMinutes(int worked, StaffShift? shift) => shift is null ? 0
        : Math.Max(0, worked - Math.Max(0, (int)(shift.EndsAtUtc - shift.StartsAtUtc).TotalMinutes
            - shift.UnpaidBreakMinutes));
}

public sealed record RecordAttendanceRequest(Guid OrganizationId, Guid BranchId, Guid StaffMemberId,
    Guid? StaffShiftId, string EventType, DateTimeOffset OccurredAtUtc, string Source = "Portal",
    string? IdempotencyKey = null);
public sealed record CorrectAttendanceRequest(DateTimeOffset CorrectedOccurredAtUtc, string Reason);
