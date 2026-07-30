using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class ConsumptionRulesTests
{
    [Fact] public void Adds_wastage_allowance() => Assert.Equal(1.05m, ConsumptionRules.WithWastage(1m, 5m));
    [Fact] public void Rejects_invalid_wastage() => Assert.Throws<ArgumentOutOfRangeException>(() => ConsumptionRules.WithWastage(1m, 101m));
}
