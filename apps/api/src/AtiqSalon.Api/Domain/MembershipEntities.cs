namespace AtiqSalon.Api.Domain;

public sealed class MembershipPlan : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal RecurringPrice { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string BillingInterval { get; set; } = "Monthly";
    public decimal IncludedCredits { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerMembership : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public Guid EnrollmentSaleId { get; set; }
    public string MembershipNumber { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTimeOffset StartsAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndsAtUtc { get; set; }
    public DateTimeOffset NextBillingAtUtc { get; set; }
}

public sealed class MembershipLedgerEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerMembershipId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string EntryType { get; set; } = "Credit";
    public decimal Credits { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string? Reference { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
