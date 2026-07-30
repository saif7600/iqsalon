namespace AtiqSalon.Api.Domain;

public sealed class BranchDailyClosing : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string Status { get; set; } = "PendingApproval";
    public string CurrencyCode { get; set; } = "AED";
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Tips { get; set; }
    public decimal PaymentsIn { get; set; }
    public decimal RefundsOut { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal CashVariance { get; set; }
    public int PostedSaleCount { get; set; }
    public int InvoiceCount { get; set; }
    public int RefundCount { get; set; }
    public string VatSummaryJson { get; set; } = "[]";
    public string PaymentSummaryJson { get; set; } = "[]";
    public string TillSummaryJson { get; set; } = "[]";
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovalNote { get; set; }
    public DateTimeOffset ClosedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAtUtc { get; set; }
}
