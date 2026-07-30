using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class PayrollInputRulesTests
{
    [Fact]
    public void Attendance_calculation_never_produces_negative_minutes()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(0, WorkforceRules.WorkedMinutes(now, now.AddMinutes(-1), 0));
        Assert.Equal(0, WorkforceRules.WorkedMinutes(now, now.AddMinutes(10), 20));
    }

    [Fact]
    public void Overtime_uses_net_scheduled_minutes()
    {
        var start = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
        var shift = new AtiqSalon.Api.Domain.StaffShift
        {
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(9),
            UnpaidBreakMinutes = 60
        };
        Assert.Equal(30, WorkforceRules.OvertimeMinutes(510, shift));
    }
}
