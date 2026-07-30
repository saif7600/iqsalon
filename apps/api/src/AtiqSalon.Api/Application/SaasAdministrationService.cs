using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace AtiqSalon.Api.Application;

public sealed class SaasAdministrationService(AppDbContext db, TenantContext tenant, IPasswordHasher<User> hasher)
{
    public async Task<object> GetOverviewAsync(CancellationToken ct)
    {
        var tenants = db.Tenants.IgnoreQueryFilters();
        var subscriptions = db.SaasSubscriptions.IgnoreQueryFilters();
        return new
        {
            tenants = await tenants.CountAsync(ct),
            activeTenants = await tenants.CountAsync(x => x.Status == "active", ct),
            plans = await db.SaasPlans.CountAsync(ct),
            activeSubscriptions = await subscriptions.CountAsync(x => x.Status == SaasSubscriptionStatuses.Active || x.Status == SaasSubscriptionStatuses.Trial, ct),
            pastDueSubscriptions = await subscriptions.CountAsync(x => x.Status == SaasSubscriptionStatuses.PastDue, ct)
        };
    }

    public async Task<object[]> GetTenantsAsync(CancellationToken ct)
    {
        var organizations = db.Organizations.IgnoreQueryFilters();
        var branches = db.Branches.IgnoreQueryFilters();
        var users = db.Users.IgnoreQueryFilters();
        var subscriptions = db.SaasSubscriptions.IgnoreQueryFilters();
        return await db.Tenants.IgnoreQueryFilters().OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.Name, x.Slug, x.Status,
            organizationCount = organizations.Count(y => y.TenantId == x.Id),
            branchCount = branches.Count(y => y.TenantId == x.Id),
            userCount = users.Count(y => y.TenantId == x.Id),
            subscriptionStatus = subscriptions.Where(y => y.TenantId == x.Id && SaasSubscriptionStatuses.Current.Contains(y.Status)).Select(y => y.Status).FirstOrDefault()
        }).Cast<object>().ToArrayAsync(ct);
    }

    public async Task<(object? Result, string? Error)> ProvisionTenantAsync(ProvisionTenantRequest request, CancellationToken ct)
    {
        var tenantName = request.TenantName.Trim();
        var legalName = request.LegalName.Trim();
        var tradingName = request.TradingName.Trim();
        var ownerName = request.OwnerName.Trim();
        var ownerEmail = request.OwnerEmail.Trim().ToLowerInvariant();
        var branchName = request.BranchName.Trim();
        var branchCode = request.BranchCode.Trim().ToUpperInvariant();
        var country = request.CountryCode.Trim().ToUpperInvariant();
        var currency = request.CurrencyCode.Trim().ToUpperInvariant();
        var language = request.Language.Trim().ToLowerInvariant();
        var timeZone = request.TimeZone.Trim();
        var billingInterval = request.BillingInterval.Trim().ToLowerInvariant();
        if (tenantName.Length is < 2 or > 120 || legalName.Length is < 2 or > 160 || tradingName.Length is < 2 or > 160)
            return (null, "Tenant, legal, and trading names are required.");
        if (ownerName.Length is < 2 or > 120 || !ownerEmail.Contains('@'))
            return (null, "A valid owner name and email are required.");
        if (branchName.Length is < 2 or > 120 || branchCode.Length is < 2 or > 20)
            return (null, "A valid initial branch name and code are required.");
        if (country.Length != 2 || currency.Length != 3 || string.IsNullOrWhiteSpace(timeZone))
            return (null, "Country, currency, and timezone are required.");
        if (request.PlanId.HasValue && !SaasBillingIntervals.All.Contains(billingInterval))
            return (null, "A valid billing interval is required.");

        var slugBase = Slug.Create(tenantName);
        if (string.IsNullOrWhiteSpace(slugBase)) return (null, "Tenant name must contain letters or numbers.");
        var slug = slugBase;
        for (var suffix = 2; await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug, ct); suffix++)
            slug = $"{slugBase[..Math.Min(slugBase.Length, 70)]}-{suffix}";
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => x.NormalizedEmail == ownerEmail, ct))
            return (null, "The owner email is already assigned to an account.");

        SaasPlan? plan = null;
        if (request.PlanId.HasValue)
        {
            plan = await db.SaasPlans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.Status == SaasPlanStatuses.Active, ct);
            if (plan is null) return (null, "The selected plan is not active.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var tenantRecord = new Tenant { Name = tenantName, Slug = slug, Status = "active" };
        var organization = new Organization
        {
            TenantId = tenantRecord.Id, LegalName = legalName, TradingName = tradingName, Slug = slug,
            Email = ownerEmail, CountryCode = country, DefaultCurrency = currency,
            DefaultLanguage = language, TimeZone = timeZone, Status = "active"
        };
        var branch = new Branch
        {
            TenantId = tenantRecord.Id, OrganizationId = organization.Id, Name = branchName,
            Code = branchCode, City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            CountryCode = country, TimeZone = timeZone, IsActive = true
        };
        var invitationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(invitationToken)));
        var owner = new User
        {
            TenantId = tenantRecord.Id, Email = ownerEmail, NormalizedEmail = ownerEmail,
            DisplayName = ownerName, Status = "invited", EmailVerified = false,
            EmailVerificationToken = invitationHash, Roles = ["OrganizationOwner"]
        };
        owner.PasswordHash = hasher.HashPassword(owner, Convert.ToHexString(RandomNumberGenerator.GetBytes(48)));
        var assignment = new UserBranchAssignment
        {
            TenantId = tenantRecord.Id, UserId = owner.Id, OrganizationId = organization.Id,
            BranchId = branch.Id, IsActive = true
        };
        var invitationPath = $"/accept-invitation?token={Uri.EscapeDataString(invitationToken)}";
        db.AddRange(
            tenantRecord, organization, branch, owner, assignment,
            new NotificationMessage
            {
                TenantId = tenantRecord.Id, OrganizationId = organization.Id, BranchId = branch.Id,
                Channel = "Email", TemplateCode = "tenant-owner-invitation", Recipient = ownerEmail,
                Subject = "Activate your AtiqSalon account",
                Body = $"You have been invited to manage {tradingName}. Set your password: {invitationPath}",
                Status = "Pending", IdempotencyKey = $"tenant-owner-invite:{owner.Id}"
            },
            new AuditEvent
            {
                TenantId = tenantRecord.Id, OrganizationId = organization.Id, ActorUserId = tenant.UserId,
                Action = "tenant.provisioned", EntityType = "Tenant", EntityId = tenantRecord.Id.ToString(),
                Source = "platform", OccurredAtUtc = DateTimeOffset.UtcNow
            });

        SaasSubscription? subscription = null;
        if (plan is not null)
        {
            var now = DateTime.UtcNow;
            var trialEnds = plan.TrialDays > 0 ? now.AddDays(plan.TrialDays) : (DateTime?)null;
            subscription = new SaasSubscription
            {
                TenantId = tenantRecord.Id, OrganizationId = organization.Id, SaasPlanId = plan.Id,
                Status = trialEnds.HasValue ? SaasSubscriptionStatuses.Trial : SaasSubscriptionStatuses.Active,
                BillingInterval = billingInterval, CurrencyCode = currency, CurrentPeriodStartUtc = now,
                CurrentPeriodEndUtc = billingInterval == SaasBillingIntervals.Annual ? now.AddYears(1) : now.AddMonths(1),
                TrialEndsAtUtc = trialEnds
            };
            db.AddRange(subscription, new SaasBillingAccount
            {
                TenantId = tenantRecord.Id, OrganizationId = organization.Id, LegalName = legalName,
                BillingEmail = ownerEmail, CurrencyCode = currency, CountryCode = country
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (new
        {
            tenantRecord.Id, tenantRecord.Name, tenantRecord.Slug, organizationId = organization.Id,
            branchId = branch.Id, ownerId = owner.Id, owner.Email, invitationPath,
            subscriptionStatus = subscription?.Status ?? "unassigned"
        }, null);
    }

    public async Task<object[]> GetPlansAsync(CancellationToken ct) =>
        await db.SaasPlans.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Select(x => new
        {
            x.Id, x.Code, x.Name, x.Description, x.Status, x.TrialDays, x.GracePeriodDays, x.DisplayOrder, x.IsPublic,
            prices = db.SaasPlanPrices.Where(y => y.SaasPlanId == x.Id && y.IsActive).OrderBy(y => y.BillingInterval)
                .Select(y => new { y.Id, y.CurrencyCode, y.BillingInterval, y.Amount }).ToArray()
        }).Cast<object>().ToArrayAsync(ct);

    public async Task<(object? Result, string? Error)> CreatePlanAsync(CreateSaasPlanRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        var status = request.Status.Trim().ToLowerInvariant();
        var interval = request.BillingInterval.Trim().ToLowerInvariant();
        var currency = request.CurrencyCode.Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 40 || request.Name.Trim().Length is < 2 or > 120) return (null, "Plan code and name are required.");
        if (!SaasPlanStatuses.All.Contains(status)) return (null, "Invalid plan status.");
        if (!SaasBillingIntervals.All.Contains(interval)) return (null, "Invalid billing interval.");
        if (request.Amount < 0 || currency.Length != 3 || request.TrialDays is < 0 or > 365 || request.GracePeriodDays is < 0 or > 90) return (null, "Invalid plan commercial terms.");
        if (await db.SaasPlans.AnyAsync(x => x.Code == code, ct)) return (null, "A plan with this code already exists.");
        var plan = new SaasPlan { Code = code, Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "", Status = status, TrialDays = request.TrialDays, GracePeriodDays = request.GracePeriodDays, DisplayOrder = request.DisplayOrder, IsPublic = request.IsPublic };
        db.SaasPlans.Add(plan);
        db.SaasPlanPrices.Add(new SaasPlanPrice { SaasPlanId = plan.Id, CurrencyCode = currency, BillingInterval = interval, Amount = request.Amount, EffectiveFromUtc = DateTime.UtcNow });
        AddAudit("saas.plan.created", "SaasPlan", plan.Id);
        await db.SaveChangesAsync(ct);
        return (new { plan.Id, plan.Code, plan.Name, plan.Status }, null);
    }

    public async Task<object[]> GetSubscriptionsAsync(CancellationToken ct) =>
        await db.SaasSubscriptions.IgnoreQueryFilters().OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            x.Id, x.TenantId,
            tenantName = db.Tenants.IgnoreQueryFilters().Where(y => y.Id == x.TenantId).Select(y => y.Name).FirstOrDefault(),
            organizationName = db.Organizations.IgnoreQueryFilters().Where(y => y.Id == x.OrganizationId).Select(y => y.TradingName).FirstOrDefault(),
            planName = db.SaasPlans.Where(y => y.Id == x.SaasPlanId).Select(y => y.Name).FirstOrDefault(),
            x.Status, x.BillingInterval, x.CurrencyCode, x.CurrentPeriodStartUtc, x.CurrentPeriodEndUtc, x.TrialEndsAtUtc
        }).Cast<object>().ToArrayAsync(ct);

    public async Task<(object? Result, string? Error)> ActivateSubscriptionAsync(ActivateSaasSubscriptionRequest request, CancellationToken ct)
    {
        var tenantRecord = await db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == request.TenantId, ct);
        var organization = await db.Organizations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == request.OrganizationId, ct);
        var plan = await db.SaasPlans.SingleOrDefaultAsync(x => x.Id == request.PlanId && x.Status == SaasPlanStatuses.Active, ct);
        var interval = request.BillingInterval.Trim().ToLowerInvariant();
        if (tenantRecord is null || organization is null || organization.TenantId != request.TenantId) return (null, "Tenant and organization do not match.");
        if (plan is null) return (null, "An active plan is required.");
        if (!SaasBillingIntervals.All.Contains(interval)) return (null, "Invalid billing interval.");
        if (await db.SaasSubscriptions.IgnoreQueryFilters().AnyAsync(x => x.TenantId == request.TenantId && SaasSubscriptionStatuses.Current.Contains(x.Status), ct)) return (null, "This tenant already has a current subscription.");
        var now = DateTime.UtcNow;
        var trialEnds = plan.TrialDays > 0 ? now.AddDays(plan.TrialDays) : (DateTime?)null;
        var subscription = new SaasSubscription
        {
            TenantId = request.TenantId, OrganizationId = request.OrganizationId, SaasPlanId = request.PlanId,
            Status = trialEnds.HasValue ? SaasSubscriptionStatuses.Trial : SaasSubscriptionStatuses.Active,
            BillingInterval = interval, CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            CurrentPeriodStartUtc = now, CurrentPeriodEndUtc = interval == SaasBillingIntervals.Annual ? now.AddYears(1) : now.AddMonths(1), TrialEndsAtUtc = trialEnds
        };
        db.SaasSubscriptions.Add(subscription);
        if (!await db.SaasBillingAccounts.IgnoreQueryFilters().AnyAsync(x => x.TenantId == request.TenantId && x.OrganizationId == request.OrganizationId, ct))
            db.SaasBillingAccounts.Add(new SaasBillingAccount { TenantId = request.TenantId, OrganizationId = request.OrganizationId, LegalName = organization.LegalName, BillingEmail = request.BillingEmail.Trim().ToLowerInvariant(), CurrencyCode = subscription.CurrencyCode });
        AddAudit("saas.subscription.activated", "SaasSubscription", subscription.Id);
        await db.SaveChangesAsync(ct);
        return (new { subscription.Id, subscription.Status, subscription.CurrentPeriodEndUtc }, null);
    }

    private void AddAudit(string action, string entityType, Guid entityId) =>
        db.AuditEvents.Add(new AuditEvent { TenantId = tenant.TenantId ?? throw new InvalidOperationException("Platform actor must belong to a tenant."), ActorUserId = tenant.UserId, Action = action, EntityType = entityType, EntityId = entityId.ToString(), OccurredAtUtc = DateTimeOffset.UtcNow });
}

public sealed record CreateSaasPlanRequest(string Code, string Name, string? Description, string Status, int TrialDays, int GracePeriodDays, int DisplayOrder, bool IsPublic, string CurrencyCode, string BillingInterval, decimal Amount);
public sealed record ActivateSaasSubscriptionRequest(Guid TenantId, Guid OrganizationId, Guid PlanId, string BillingInterval, string CurrencyCode, string BillingEmail);
public sealed record ProvisionTenantRequest(string TenantName, string LegalName, string TradingName, string OwnerName, string OwnerEmail, string BranchName, string BranchCode, string? City, string CountryCode, string CurrencyCode, string Language, string TimeZone, Guid? PlanId, string BillingInterval);
