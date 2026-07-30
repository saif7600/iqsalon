using AtiqSalon.Api.Application;

namespace AtiqSalon.Api.Tests;

public sealed class AiGovernanceRulesTests
{
    [Theory]
    [InlineData("Disabled")]
    [InlineData("Trial")]
    [InlineData("Active")]
    [InlineData("Paused")]
    [InlineData("BudgetExceeded")]
    [InlineData("Suspended")]
    public void Only_documented_ai_statuses_are_accepted(string status) =>
        Assert.True(AiGovernanceRules.ValidStatus(status));

    [Fact]
    public void Unknown_processing_mode_is_rejected() =>
        Assert.False(AiGovernanceRules.ValidProcessingMode("GuaranteedZeroRetention"));
}
