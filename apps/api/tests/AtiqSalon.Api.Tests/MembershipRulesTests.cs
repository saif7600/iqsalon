using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class MembershipRulesTests
{
    [Fact]
    public void Monthly_billing_uses_calendar_months()
    {
        var start = new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero),
            MembershipRules.NextBilling(start, "Monthly"));
    }

    [Fact]
    public void Supported_intervals_advance_correctly()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(start.AddDays(7), MembershipRules.NextBilling(start, "Weekly"));
        Assert.Equal(start.AddMonths(3), MembershipRules.NextBilling(start, "Quarterly"));
        Assert.Equal(start.AddYears(1), MembershipRules.NextBilling(start, "Annual"));
    }
}
