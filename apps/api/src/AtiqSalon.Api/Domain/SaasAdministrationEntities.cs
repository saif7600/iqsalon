namespace AtiqSalon.Api.Domain;

public sealed class SaasPlan : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = SaasPlanStatuses.Draft;
    public int TrialDays { get; set; }
    public int GracePeriodDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublic { get; set; }
}

public sealed class SaasPlanPrice : Entity
{
    public Guid SaasPlanId { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string BillingInterval { get; set; } = SaasBillingIntervals.Monthly;
    public decimal Amount { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaasSubscription : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SaasPlanId { get; set; }
    public string Status { get; set; } = SaasSubscriptionStatuses.Trial;
    public string BillingInterval { get; set; } = SaasBillingIntervals.Monthly;
    public string CurrencyCode { get; set; } = "AED";
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime? GracePeriodEndsAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class SaasBillingAccount : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "AED";
    public string CountryCode { get; set; } = "AE";
    public string? TaxRegistrationNumber { get; set; }
}

public static class SaasPlanStatuses
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Retired = "retired";
    public static readonly string[] All = [Draft, Active, Retired];
}

public static class SaasSubscriptionStatuses
{
    public const string Trial = "trial";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
    public static readonly string[] Current = [Trial, Active, PastDue, Suspended];
}

public static class SaasBillingIntervals
{
    public const string Monthly = "monthly";
    public const string Annual = "annual";
    public static readonly string[] All = [Monthly, Annual];
}
