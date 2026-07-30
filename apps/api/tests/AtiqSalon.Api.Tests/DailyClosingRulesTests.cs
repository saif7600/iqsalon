using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class DailyClosingRulesTests
{
    [Fact]
    public void Dubai_business_day_maps_to_correct_utc_range()
    {
        var (from, to) = DailyClosingRules.UtcRange(new DateOnly(2026, 7, 30), "Asia/Dubai");
        Assert.Equal(new DateTimeOffset(2026, 7, 29, 20, 0, 0, TimeSpan.Zero), from);
        Assert.Equal(TimeSpan.FromHours(24), to - from);
    }
}
