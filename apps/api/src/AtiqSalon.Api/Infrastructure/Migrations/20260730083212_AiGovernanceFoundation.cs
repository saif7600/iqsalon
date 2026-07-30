using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtiqSalon.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AiGovernanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModelDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ModelCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CapabilityJson = table.Column<string>(type: "text", nullable: false),
                    ContextWindow = table.Column<int>(type: "integer", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStructuredOutput = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsEmbeddings = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    InputCostPerMillion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OutputCostPerMillion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiPromptDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    DeveloperPrompt = table.Column<string>(type: "text", nullable: false),
                    AllowedToolCodesJson = table.Column<string>(type: "text", nullable: false),
                    OutputSchemaJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiRoutingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    PrimaryProvider = table.Column<string>(type: "text", nullable: false),
                    PrimaryModel = table.Column<string>(type: "text", nullable: false),
                    FallbackProvider = table.Column<string>(type: "text", nullable: true),
                    FallbackModel = table.Column<string>(type: "text", nullable: true),
                    MaximumInputTokens = table.Column<int>(type: "integer", nullable: false),
                    MaximumOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MonthlyBudgetShare = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRoutingPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    PromptDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PromptVersion = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InputTokenCount = table.Column<int>(type: "integer", nullable: false),
                    OutputTokenCount = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    RequestMetadataJson = table.Column<string>(type: "text", nullable: false),
                    SafetyResultJson = table.Column<string>(type: "text", nullable: false),
                    GroundingResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAiSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DefaultProvider = table.Column<string>(type: "text", nullable: false),
                    DefaultModel = table.Column<string>(type: "text", nullable: false),
                    FallbackProvider = table.Column<string>(type: "text", nullable: true),
                    FallbackModel = table.Column<string>(type: "text", nullable: true),
                    DataProcessingMode = table.Column<string>(type: "text", nullable: false),
                    AllowCustomerFacingAi = table.Column<bool>(type: "boolean", nullable: false),
                    AllowInternalCopilot = table.Column<bool>(type: "boolean", nullable: false),
                    AllowToolExecution = table.Column<bool>(type: "boolean", nullable: false),
                    AllowKnowledgeRetrieval = table.Column<bool>(type: "boolean", nullable: false),
                    AllowConversationStorage = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPromptLogging = table.Column<bool>(type: "boolean", nullable: false),
                    AllowResponseLogging = table.Column<bool>(type: "boolean", nullable: false),
                    AllowSensitiveDataUsage = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyBudgetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyTokenLimit = table.Column<long>(type: "bigint", nullable: false),
                    DailyRequestLimit = table.Column<int>(type: "integer", nullable: false),
                    PerUserDailyLimit = table.Column<int>(type: "integer", nullable: false),
                    RequireApprovalForCustomerMessages = table.Column<bool>(type: "boolean", nullable: false),
                    RequireApprovalForHighRiskActions = table.Column<bool>(type: "boolean", nullable: false),
                    ConcurrencyToken = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAiSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiModelDefinitions_Provider_ModelCode",
                table: "AiModelDefinitions",
                columns: new[] { "Provider", "ModelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptDefinitions_TenantId_OrganizationId_Code_Version",
                table: "AiPromptDefinitions",
                columns: new[] { "TenantId", "OrganizationId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiRoutingPolicies_TenantId_OrganizationId_UseCase",
                table: "AiRoutingPolicies",
                columns: new[] { "TenantId", "OrganizationId", "UseCase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageEntries_TenantId_IdempotencyKey",
                table: "AiUsageEntries",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageEntries_TenantId_OrganizationId_UsageDate",
                table: "AiUsageEntries",
                columns: new[] { "TenantId", "OrganizationId", "UsageDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAiSettings_TenantId_OrganizationId",
                table: "TenantAiSettings",
                columns: new[] { "TenantId", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiModelDefinitions");

            migrationBuilder.DropTable(
                name: "AiPromptDefinitions");

            migrationBuilder.DropTable(
                name: "AiRoutingPolicies");

            migrationBuilder.DropTable(
                name: "AiRuns");

            migrationBuilder.DropTable(
                name: "AiUsageEntries");

            migrationBuilder.DropTable(
                name: "TenantAiSettings");
        }
    }
}
