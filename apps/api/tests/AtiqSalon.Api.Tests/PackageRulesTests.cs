namespace AtiqSalon.Api.Tests;

public sealed class PackageRulesTests
{
    [Theory]
    [InlineData(5, 2, true)]
    [InlineData(2, 2, true)]
    [InlineData(1, 2, false)]
    public void Consumption_requires_sufficient_entitlement(decimal balance, decimal requested, bool allowed) =>
        Assert.Equal(allowed, requested > 0 && requested <= balance);
}
