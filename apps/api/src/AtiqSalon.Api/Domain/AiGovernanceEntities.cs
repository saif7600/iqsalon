namespace AtiqSalon.Api.Domain;

public sealed class TenantAiSettings : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Status { get; set; } = "Disabled";
    public string DefaultProvider { get; set; } = "Simulation";
    public string DefaultModel { get; set; } = "simulation-v1";
    public string? FallbackProvider { get; set; }
    public string? FallbackModel { get; set; }
    public string DataProcessingMode { get; set; } = "LocalSimulation";
    public bool AllowCustomerFacingAi { get; set; }
    public bool AllowInternalCopilot { get; set; }
    public bool AllowToolExecution { get; set; }
    public bool AllowKnowledgeRetrieval { get; set; }
    public bool AllowConversationStorage { get; set; }
    public bool AllowPromptLogging { get; set; }
    public bool AllowResponseLogging { get; set; }
    public bool AllowSensitiveDataUsage { get; set; }
    public decimal MonthlyBudgetAmount { get; set; }
    public long MonthlyTokenLimit { get; set; }
    public int DailyRequestLimit { get; set; } = 100;
    public int PerUserDailyLimit { get; set; } = 25;
    public bool RequireApprovalForCustomerMessages { get; set; } = true;
    public bool RequireApprovalForHighRiskActions { get; set; } = true;
    public long ConcurrencyToken { get; set; }
}

public sealed class AiModelDefinition : Entity
{
    public string Provider { get; set; } = "";
    public string ModelCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string CapabilityJson { get; set; } = "{}";
    public int ContextWindow { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsStructuredOutput { get; set; }
    public bool SupportsStreaming { get; set; }
    public bool SupportsEmbeddings { get; set; }
    public bool SupportsVision { get; set; }
    public decimal InputCostPerMillion { get; set; }
    public decimal OutputCostPerMillion { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class AiRoutingPolicy : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string UseCase { get; set; } = "";
    public string PrimaryProvider { get; set; } = "";
    public string PrimaryModel { get; set; } = "";
    public string? FallbackProvider { get; set; }
    public string? FallbackModel { get; set; }
    public int MaximumInputTokens { get; set; } = 4000;
    public int MaximumOutputTokens { get; set; } = 1000;
    public decimal Temperature { get; set; } = 0.2m;
    public int TimeoutSeconds { get; set; } = 45;
    public int RetryCount { get; set; }
    public decimal MonthlyBudgetShare { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed class AiPromptDefinition : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string UseCase { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public string DeveloperPrompt { get; set; } = "";
    public string AllowedToolCodesJson { get; set; } = "[]";
    public string? OutputSchemaJson { get; set; }
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class AiRun : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ConversationId { get; set; }
    public string UseCase { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public Guid? PromptDefinitionId { get; set; }
    public int? PromptVersion { get; set; }
    public string Status { get; set; } = "Queued";
    public int InputTokenCount { get; set; }
    public int OutputTokenCount { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
    public string CorrelationId { get; set; } = Guid.CreateVersion7().ToString("N");
    public string RequestMetadataJson { get; set; } = "{}";
    public string SafetyResultJson { get; set; } = "{}";
    public string GroundingResultJson { get; set; } = "{}";
}

public sealed class AiUsageEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid AiRunId { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateOnly UsageDate { get; set; }
    public string IdempotencyKey { get; set; } = "";
}
