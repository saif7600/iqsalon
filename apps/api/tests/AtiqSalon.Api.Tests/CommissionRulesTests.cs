using AtiqSalon.Api.Application;
using AtiqSalon.Api.Domain;

namespace AtiqSalon.Api.Tests;

public sealed class CommissionRulesTests
{
    [Fact]
    public void Net_revenue_commission_uses_discounted_amount()
    {
        var line = new SaleLine { GrossAmount = 100, DiscountAmount = 20, NetAmount = 80 };
        Assert.Equal(80, CommissionRules.Basis(line, "NetRevenue"));
        Assert.Equal(8, CommissionRules.Calculate(80, 10));
    }

    [Fact]
    public void Gross_profit_never_becomes_negative()
    {
        var line = new SaleLine { NetAmount = 40, CostSnapshot = 50, Quantity = 1 };
        Assert.Equal(0, CommissionRules.Basis(line, "GrossProfit"));
    }
}
