using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class CommercialControlRulesTests
{
    [Fact]
    public void Refund_available_never_exceeds_unreturned_paid_amount()
    {
        Assert.Equal(65m, CommercialControlRules.AvailableRefund(100m, 35m));
        Assert.Equal(0m, CommercialControlRules.AvailableRefund(100m, 125m));
    }

    [Fact]
    public void Till_variance_is_counted_less_expected()
    {
        Assert.Equal(-12.35m, CommercialControlRules.TillVariance(512.35m, 500m));
        Assert.Equal(7.55m, CommercialControlRules.TillVariance(500m, 507.55m));
    }
}
