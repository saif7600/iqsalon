namespace AtiqSalon.Api.Domain;

public sealed class PackageDefinition : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public int ValidityDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
}

public sealed class PackageEntitlement : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PackageDefinitionId { get; set; }
    public Guid ServiceId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class CustomerPackage : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PackageDefinitionId { get; set; }
    public Guid SaleId { get; set; }
    public string PackageNumber { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTimeOffset PurchasedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class PackageLedgerEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerPackageId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string EntryType { get; set; } = "Credit";
    public decimal Quantity { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
