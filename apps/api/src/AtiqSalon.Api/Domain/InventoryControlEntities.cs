namespace AtiqSalon.Api.Domain;

public sealed class Stocktake : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public string StocktakeNumber { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateOnly BusinessDate { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }
}

public sealed class StocktakeLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StocktakeId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
}

public sealed class WastageRecord : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public string WastageNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? Notes { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? PostedAtUtc { get; set; }
}

public sealed class ExpenseRecord : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string ExpenseNumber { get; set; } = "";
    public string ExpenseType { get; set; } = "OperatingExpense";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string PaymentSource { get; set; } = "AccountsPayable";
    public string Status { get; set; } = "Draft";
    public DateOnly ExpenseDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? PaidByUserId { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
}
