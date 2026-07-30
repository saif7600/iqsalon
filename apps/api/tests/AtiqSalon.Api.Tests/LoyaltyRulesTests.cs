using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class LoyaltyRulesTests
{
    [Fact]
    public void Organization_owner_receives_iqai_and_loyalty_permissions()
    {
        var permissions = AtiqSalon.Api.Security.PermissionCatalog.ForRoles(["OrganizationOwner"]);
        Assert.Contains("iqai.use", permissions);
        Assert.Contains("loyalty.adjust", permissions);
    }

    [Fact]
    public void Service_provider_cannot_adjust_customer_points()
    {
        Assert.DoesNotContain("loyalty.adjust",
            AtiqSalon.Api.Security.PermissionCatalog.ForRoles(["ServiceProvider"]));
    }
}
