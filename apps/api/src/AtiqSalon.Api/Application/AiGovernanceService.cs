using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class AiGovernanceService(AppDbContext db, TenantContext tenant)
{
    public async Task<(bool Allowed, string Code)> CheckAdmissionAsync(
        Guid organizationId, Guid userId, CancellationToken ct)
    {
        var settings = await db.TenantAiSettings.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId, ct);
        if (settings is null || settings.Status is not ("Active" or "Trial"))
            return (false, "ai_disabled");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var daily = await db.AiUsageEntries.CountAsync(x =>
            x.OrganizationId == organizationId && x.UsageDate == today, ct);
        if (daily >= settings.DailyRequestLimit) return (false, "daily_limit");
        var userDaily = await db.AiUsageEntries.CountAsync(x =>
            x.OrganizationId == organizationId && x.UserId == userId && x.UsageDate == today, ct);
        if (userDaily >= settings.PerUserDailyLimit) return (false, "user_daily_limit");
        var monthly = await db.AiUsageEntries.Where(x =>
            x.OrganizationId == organizationId && x.UsageDate >= monthStart)
            .GroupBy(_ => 1).Select(x => new
            {
                Tokens = x.Sum(y => y.InputTokens + y.OutputTokens),
                Cost = x.Sum(y => y.EstimatedCost)
            }).SingleOrDefaultAsync(ct);
        if (settings.MonthlyTokenLimit > 0 && (monthly?.Tokens ?? 0) >= settings.MonthlyTokenLimit)
            return (false, "token_budget");
        if (settings.MonthlyBudgetAmount > 0 && (monthly?.Cost ?? 0) >= settings.MonthlyBudgetAmount)
            return (false, "cost_budget");
        return (true, "allowed");
    }

    public async Task<TenantAiSettings> UpdateSettingsAsync(Guid organizationId,
        UpdateAiSettingsRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null) throw new UnauthorizedAccessException();
        if (!AiGovernanceRules.ValidStatus(request.Status)
            || !AiGovernanceRules.ValidProcessingMode(request.DataProcessingMode)
            || request.MonthlyBudgetAmount < 0 || request.MonthlyTokenLimit < 0
            || request.DailyRequestLimit < 1 || request.PerUserDailyLimit < 1)
            throw new ArgumentException("AI settings are invalid.");
        var item = await db.TenantAiSettings.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId, ct);
        item ??= new TenantAiSettings
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = organizationId
        };
        if (db.Entry(item).State == EntityState.Detached) db.TenantAiSettings.Add(item);
        item.Status = request.Status;
        item.DefaultProvider = request.DefaultProvider.Trim();
        item.DefaultModel = request.DefaultModel.Trim();
        item.DataProcessingMode = request.DataProcessingMode;
        item.AllowCustomerFacingAi = request.AllowCustomerFacingAi;
        item.AllowInternalCopilot = request.AllowInternalCopilot;
        item.AllowToolExecution = request.AllowToolExecution;
        item.AllowKnowledgeRetrieval = request.AllowKnowledgeRetrieval;
        item.MonthlyBudgetAmount = request.MonthlyBudgetAmount;
        item.MonthlyTokenLimit = request.MonthlyTokenLimit;
        item.DailyRequestLimit = request.DailyRequestLimit;
        item.PerUserDailyLimit = request.PerUserDailyLimit;
        item.ConcurrencyToken++;
        await db.SaveChangesAsync(ct);
        return item;
    }
}

public static class AiGovernanceRules
{
    public static bool ValidStatus(string value) =>
        value is "Disabled" or "Trial" or "Active" or "Paused"
            or "BudgetExceeded" or "Suspended";
    public static bool ValidProcessingMode(string value) =>
        value is "ProviderStandard" or "NoRetentionRequested"
            or "PrivateDeployment" or "LocalSimulation";
}

public sealed record UpdateAiSettingsRequest(string Status, string DefaultProvider,
    string DefaultModel, string DataProcessingMode, bool AllowCustomerFacingAi,
    bool AllowInternalCopilot, bool AllowToolExecution, bool AllowKnowledgeRetrieval,
    decimal MonthlyBudgetAmount, long MonthlyTokenLimit, int DailyRequestLimit,
    int PerUserDailyLimit);
