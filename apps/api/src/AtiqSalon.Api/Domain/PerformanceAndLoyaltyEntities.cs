namespace AtiqSalon.Api.Domain;

public sealed class PerformanceTarget : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? StaffMemberId { get; set; }
    public string Metric { get; set; } = "";
    public decimal TargetValue { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = "Active";
    public Guid CreatedByUserId { get; set; }
}

public sealed class PerformanceReview : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal Rating { get; set; }
    public string Summary { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public Guid ReviewerUserId { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
}

public sealed class LoyaltyProgram : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public decimal PointsPerCurrencyUnit { get; set; } = 1;
    public decimal RedemptionValuePerPoint { get; set; } = 0.01m;
    public bool IsActive { get; set; } = true;
}

public sealed class LoyaltyTier : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LoyaltyProgramId { get; set; }
    public string Name { get; set; } = "";
    public decimal MinimumLifetimePoints { get; set; }
    public decimal EarningMultiplier { get; set; } = 1;
    public int Rank { get; set; }
}

public sealed class CustomerLoyaltyAccount : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid LoyaltyProgramId { get; set; }
    public Guid? LoyaltyTierId { get; set; }
    public decimal PointsBalance { get; set; }
    public decimal LifetimePoints { get; set; }
}

public sealed class LoyaltyLedgerEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerLoyaltyAccountId { get; set; }
    public Guid? SaleId { get; set; }
    public string EntryType { get; set; } = "Earn";
    public decimal Points { get; set; }
    public string Reason { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
}

public sealed class ReferralCode : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public string Code { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerReferral : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ReferralCodeId { get; set; }
    public Guid ReferrerCustomerId { get; set; }
    public Guid ReferredCustomerId { get; set; }
    public string Status { get; set; } = "Qualified";
    public DateTimeOffset QualifiedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
