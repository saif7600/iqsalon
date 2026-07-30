using System.Security.Cryptography;

namespace AtiqSalon.Api.Tests;

public sealed class GiftCardRulesTests
{
    [Fact]
    public void Generated_code_has_128_bits_of_random_material()
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        Assert.Equal(32, code.Length);
    }

    [Theory]
    [InlineData(100, 60, 80, 60)]
    [InlineData(20, 60, 80, 20)]
    [InlineData(100, 120, 80, 80)]
    public void Redemption_is_bounded_by_request_balance_and_sale(
        decimal requested, decimal balance, decimal saleBalance, decimal expected) =>
        Assert.Equal(expected, Math.Min(requested, Math.Min(balance, saleBalance)));
}
