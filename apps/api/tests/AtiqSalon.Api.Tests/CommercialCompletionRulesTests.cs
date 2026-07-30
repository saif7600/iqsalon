using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class CommercialCompletionRulesTests
{
    [Fact]
    public void Inclusive_tax_is_recalculated_after_approved_discount()
    {
        var result = CommercialRules.CalculateLine(1, 105, 21, 5, true);
        Assert.Equal(84, result.Net);
        Assert.Equal(4, result.Tax);
        Assert.Equal(80, result.Taxable);
    }
}
