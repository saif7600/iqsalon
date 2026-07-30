using AtiqSalon.Api.Application;
using AtiqSalon.Api.Domain;

namespace AtiqSalon.Api.Tests;

public sealed class WorkforceRulesTests
{
    [Fact]
    public void Worked_minutes_exclude_breaks()
    {
        var start = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
        Assert.Equal(450, WorkforceRules.WorkedMinutes(start, start.AddHours(8), 30));
    }

    [Fact]
    public void Unpaired_break_is_not_counted()
    {
        var start = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
        Assert.Equal(20, WorkforceRules.BreakMinutes([
            new AttendanceEvent { EventType = "BreakStart", OccurredAtUtc = start },
            new AttendanceEvent { EventType = "BreakEnd", OccurredAtUtc = start.AddMinutes(20) },
            new AttendanceEvent { EventType = "BreakStart", OccurredAtUtc = start.AddMinutes(60) }
        ]));
    }

    [Fact]
    public void Grace_is_removed_from_lateness()
    {
        var start = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
        Assert.Equal(7, WorkforceRules.LateMinutes(start.AddMinutes(12), start, 5));
    }
}
