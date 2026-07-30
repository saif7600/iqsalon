using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class DepositRulesTests
{
    [Theory]
    [InlineData(100, 80, 50, 50)]
    [InlineData(30, 80, 50, 30)]
    [InlineData(100, 20, 50, 20)]
    public void Applied_amount_is_bounded_by_request_deposit_and_sale(
        decimal requested, decimal available, decimal balance, decimal expected)
    {
        Assert.Equal(expected, CommercialRules.Round(Math.Min(requested, Math.Min(available, balance))));
    }
}
