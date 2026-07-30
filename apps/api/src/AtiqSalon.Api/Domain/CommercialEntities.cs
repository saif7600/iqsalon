namespace AtiqSalon.Api.Domain;

public sealed class OrganizationCommercialSettings : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string DefaultCurrencyCode { get; set; } = "AED";
    public bool PricesIncludeTax { get; set; } = true;
    public string InvoicePrefix { get; set; } = "INV";
    public string CreditNotePrefix { get; set; } = "CRN";
    public string ReceiptPrefix { get; set; } = "REC";
    public long NextSaleSequence { get; set; } = 1;
    public long NextInvoiceSequence { get; set; } = 1;
    public long NextCreditNoteSequence { get; set; } = 1;
    public long NextReceiptSequence { get; set; } = 1;
    public long NextPaymentSequence { get; set; } = 1;
    public long NextDepositSequence { get; set; } = 1;
    public long NextPackageSequence { get; set; } = 1;
    public long NextMembershipSequence { get; set; } = 1;
    public long NextGiftCardSequence { get; set; } = 1;
    public bool AllowSplitPayments { get; set; } = true;
    public bool AllowPartialPayments { get; set; }
    public bool AllowOverpayment { get; set; }
    public bool RequireTillSessionForCashPayments { get; set; } = true;
    public bool AllowTips { get; set; } = true;
    public decimal RequireManagerForDiscountAbovePercentage { get; set; } = 10m;
}

public sealed class TaxCode : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal RatePercentage { get; set; }
    public string TaxType { get; set; } = "Standard";
    public bool IsInclusive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductCategory : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string? LocalizedNameJson { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Product : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? LocalizedNameJson { get; set; }
    public string? Description { get; set; }
    public string Sku { get; set; } = "";
    public string? Barcode { get; set; }
    public string? Brand { get; set; }
    public string UnitOfMeasure { get; set; } = "Piece";
    public decimal RetailPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string ProductType { get; set; } = "Retail";
    public Guid? BaseUnitOfMeasureId { get; set; }
    public Guid? PurchaseUnitOfMeasureId { get; set; }
    public Guid? SaleUnitOfMeasureId { get; set; }
    public decimal StandardCost { get; set; }
    public decimal LastPurchaseCost { get; set; }
    public decimal AverageCost { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public Guid? TaxCodeId { get; set; }
    public bool TrackInventory { get; set; }
    public bool TrackBatches { get; set; }
    public bool TrackExpiry { get; set; }
    public bool TrackSerialNumbers { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool CommissionEligible { get; set; }
    public bool IsRetail { get; set; } = true;
    public bool IsProfessionalUse { get; set; }
    public bool IsConsumable { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal MaximumStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal ReorderQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BranchProduct : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal? RetailPriceOverride { get; set; }
    public Guid? TaxCodeOverrideId { get; set; }
    public bool IsAvailableForSale { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class Sale : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string SaleNumber { get; set; } = "";
    public Guid? AppointmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Status { get; set; } = "Draft";
    public string Source { get; set; } = "WalkIn";
    public string CurrencyCode { get; set; } = "AED";
    public DateOnly BusinessDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxableTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal TipTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal BalanceDue { get; set; }
    public decimal ChangeGiven { get; set; }
    public decimal RoundingAdjustment { get; set; }
    public string? CustomerSnapshotJson { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTimeOffset? PostedAtUtc { get; set; }
    public string? PostingIdempotencyKey { get; set; }
}

public sealed class SaleLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SaleId { get; set; }
    public string LineType { get; set; } = "Service";
    public Guid? ServiceId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? AppointmentServiceId { get; set; }
    public string DescriptionSnapshot { get; set; } = "";
    public string? SkuSnapshot { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string TaxCodeSnapshot { get; set; } = "OUT";
    public decimal TaxRateSnapshot { get; set; }
    public bool TaxInclusiveSnapshot { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? AssignedStaffMemberId { get; set; }
    public bool CommissionEligible { get; set; }
    public decimal CostSnapshot { get; set; }
    public int Sequence { get; set; }
}

public sealed class PaymentMethod : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Other";
    public bool RequiresReference { get; set; }
    public bool RequiresTillSession { get; set; }
    public bool SupportsRefund { get; set; } = true;
    public bool SupportsChange { get; set; }
    public bool SupportsPartialPayment { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public sealed class Payment : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string PaymentNumber { get; set; } = "";
    public Guid? CustomerId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string Direction { get; set; } = "Inbound";
    public string Status { get; set; } = "Completed";
    public string CurrencyCode { get; set; } = "AED";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string Provider { get; set; } = "Manual";
    public string? ProviderTransactionId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public Guid? ReceivedByUserId { get; set; }
    public Guid? TillSessionId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PaymentAllocation : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? DepositId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class Invoice : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SaleId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string CurrencyCode { get; set; } = "AED";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string TaxSummaryJson { get; set; } = "{}";
    public string Status { get; set; } = "Issued";
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TillSession : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public string Status { get; set; } = "Open";
    public decimal OpeningFloat { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? Variance { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? VarianceApprovedByUserId { get; set; }
    public DateTimeOffset? VarianceApprovedAtUtc { get; set; }
    public string? VarianceApprovalNote { get; set; }
}

public sealed class CashMovement : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TillSessionId { get; set; }
    public string Type { get; set; } = "CashIn";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DiscountApprovalRequest : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SaleId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal RequestedPercentage { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public Guid RequestedByUserId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public DateTimeOffset? AppliedAtUtc { get; set; }
}

public sealed class CreditNote : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SaleId { get; set; }
    public Guid InvoiceId { get; set; }
    public string CreditNoteNumber { get; set; } = "";
    public string CurrencyCode { get; set; } = "AED";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Issued";
    public Guid IssuedByUserId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Refund : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SaleId { get; set; }
    public Guid CreditNoteId { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public Guid RefundedByUserId { get; set; }
    public DateTimeOffset RefundedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomerDeposit : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PaymentId { get; set; }
    public string DepositNumber { get; set; } = "";
    public string CurrencyCode { get; set; } = "AED";
    public decimal OriginalAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public string Status { get; set; } = "Available";
}

public sealed class DepositApplication : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid DepositId { get; set; }
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public Guid AppliedByUserId { get; set; }
    public DateTimeOffset AppliedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
