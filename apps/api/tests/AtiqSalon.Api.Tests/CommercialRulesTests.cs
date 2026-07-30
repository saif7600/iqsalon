using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class CommercialRulesTests
{
    [Fact]
    public void Inclusive_vat_preserves_display_total()
    {
        var result = CommercialRules.CalculateLine(1, 105m, 0, 5m, true);
        Assert.Equal(100m, result.Taxable);
        Assert.Equal(5m, result.Tax);
        Assert.Equal(105m, result.Total);
    }

    [Fact]
    public void Exclusive_vat_adds_tax_after_discount()
    {
        var result = CommercialRules.CalculateLine(2, 50m, 10m, 5m, false);
        Assert.Equal(100m, result.Gross);
        Assert.Equal(90m, result.Taxable);
        Assert.Equal(4.5m, result.Tax);
        Assert.Equal(94.5m, result.Total);
    }

    [Fact]
    public void Discount_cannot_make_a_line_negative()
    {
        var result = CommercialRules.CalculateLine(1, 20m, 100m, 5m, false);
        Assert.Equal(20m, result.Discount);
        Assert.Equal(0m, result.Total);
    }
}
