namespace AtiqSalon.Api.Domain;

public sealed class Supplier : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string SupplierNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxRegistrationNumber { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public int PaymentTermsDays { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class PurchaseOrder : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public string PurchaseOrderNumber { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string? SupplierReference { get; set; }
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
}

public sealed class PurchaseOrderLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public int Sequence { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal ConversionFactorToBase { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTax { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class GoodsReceipt : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateOnly ReceiptDate { get; set; }
    public string? SupplierDeliveryNote { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTimeOffset? PostedAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = "";
}

public sealed class GoodsReceiptLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public int Sequence { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitCost { get; set; }
}
