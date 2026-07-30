using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class InventoryRulesTests
{
    [Fact]
    public void Weighted_average_is_deterministic()
    {
        Assert.Equal(12.6667m, InventoryRules.WeightedAverage(10, 10, 5, 18, 4));
    }

    [Fact]
    public void Standard_cost_is_selected_for_outbound()
    {
        Assert.Equal(15m, InventoryRules.OutboundCost("StandardCost", 12m, 15m));
        Assert.Equal(12m, InventoryRules.OutboundCost("WeightedAverage", 12m, 15m));
    }

    [Fact]
    public void Weighted_average_rejects_non_positive_receipt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InventoryRules.WeightedAverage(10, 10, 0, 18, 4));
    }
}
