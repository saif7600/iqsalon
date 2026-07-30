namespace AtiqSalon.Api.Domain;

public sealed class WorkforceSettings : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public int StandardDailyMinutes { get; set; } = 480;
    public int GraceMinutes { get; set; } = 5;
    public int RoundingMinutes { get; set; } = 1;
    public bool RequireAttendanceLocation { get; set; }
}

public sealed class ShiftTemplate : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int UnpaidBreakMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffShift : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid? ShiftTemplateId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public int UnpaidBreakMinutes { get; set; }
    public string Status { get; set; } = "Published";
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class AttendanceEvent : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid? StaffShiftId { get; set; }
    public Guid? CorrectsEventId { get; set; }
    public string EventType { get; set; } = "";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid RecordedByUserId { get; set; }
    public string Source { get; set; } = "Portal";
    public string? Reason { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class AttendanceRecord : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid? StaffShiftId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset? ClockInAtUtc { get; set; }
    public DateTimeOffset? ClockOutAtUtc { get; set; }
    public int WorkedMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset RecalculatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
