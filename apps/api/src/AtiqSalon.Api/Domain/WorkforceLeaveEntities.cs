namespace AtiqSalon.Api.Domain;

public sealed class LeaveType : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsPaid { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public decimal DefaultAnnualDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffLeaveBalance : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal OpeningDays { get; set; }
    public decimal AccruedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal AdjustedDays { get; set; }
    public decimal AvailableDays => OpeningDays + AccruedDays + AdjustedDays - UsedDays;
}

public sealed class LeaveRequest : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public decimal RequestedDays { get; set; }
    public string Status { get; set; } = "Pending";
    public string? StaffNote { get; set; }
    public string? DecisionNote { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class OrganizationHoliday : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = "";
    public bool IsPaid { get; set; } = true;
}

public sealed class ShiftSwapRequest : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid OfferedShiftId { get; set; }
    public Guid OfferedByStaffMemberId { get; set; }
    public Guid RequestedStaffMemberId { get; set; }
    public Guid? RequestedShiftId { get; set; }
    public string Status { get; set; } = "AwaitingRecipient";
    public bool RecipientAccepted { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class AttendanceApproval : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid AttendanceRecordId { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ApprovedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }
}

public sealed class PayrollInputBatch : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
}

public sealed class PayrollInputLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PayrollInputBatchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public int WorkedMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TipsAmount { get; set; }
    public decimal AllowanceAmount { get; set; }
    public decimal DeductionAmount { get; set; }
}
