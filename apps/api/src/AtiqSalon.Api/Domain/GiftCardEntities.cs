namespace AtiqSalon.Api.Domain;

public sealed class GiftCard : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid IssuanceSaleId { get; set; }
    public string GiftCardNumber { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public string CodeLastFour { get; set; } = "";
    public string CurrencyCode { get; set; } = "AED";
    public decimal InitialValue { get; set; }
    public string Status { get; set; } = "Active";
    public Guid? CustomerId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

public sealed class GiftCardLedgerEntry : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid GiftCardId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? PaymentId { get; set; }
    public string EntryType { get; set; } = "Issue";
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
