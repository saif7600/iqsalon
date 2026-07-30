namespace AtiqSalon.Api.Domain;

public sealed class CommissionPlan : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Basis { get; set; } = "NetRevenue";
    public decimal ServiceRatePercentage { get; set; }
    public decimal ProductRatePercentage { get; set; }
    public bool IncludeTips { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffCommissionAssignment : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid CommissionPlanId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CommissionLedgerEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid CommissionPlanId { get; set; }
    public Guid SaleId { get; set; }
    public Guid? SaleLineId { get; set; }
    public Guid? RefundId { get; set; }
    public string EntryType { get; set; } = "Earned";
    public string Basis { get; set; } = "NetRevenue";
    public decimal BasisAmount { get; set; }
    public decimal RatePercentage { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
