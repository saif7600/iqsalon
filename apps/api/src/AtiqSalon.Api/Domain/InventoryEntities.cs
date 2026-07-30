namespace AtiqSalon.Api.Domain;

public sealed class UnitOfMeasure : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string UnitType { get; set; } = "Count";
    public int DecimalPrecision { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductUnitConversion : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid FromUnitOfMeasureId { get; set; }
    public Guid ToUnitOfMeasureId { get; set; }
    public decimal ConversionFactor { get; set; }
    public bool IsPurchaseConversion { get; set; }
    public bool IsSaleConversion { get; set; }
}

public sealed class InventoryLocation : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string LocationType { get; set; } = "MainStock";
    public Guid? ParentLocationId { get; set; }
    public bool IsSellable { get; set; } = true;
    public bool IsConsumable { get; set; } = true;
    public bool IsQuarantine { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryCostSettings : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string CostingMethod { get; set; } = "WeightedAverage";
    public bool AllowNegativeStock { get; set; }
    public string NegativeStockCostPolicy { get; set; } = "RejectTransaction";
    public int CostRoundingPrecision { get; set; } = 4;
}

public sealed class ProductBatch : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; } = "";
    public Guid? SupplierId { get; set; }
    public DateTimeOffset? ManufacturedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class StockMovement : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public string MovementType { get; set; } = "";
    public string Direction { get; set; } = "Inbound";
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string CurrencyCode { get; set; } = "AED";
    public string ReferenceType { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly BusinessDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}

public sealed class InventoryBalance : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal AverageUnitCost { get; set; }
    public DateTimeOffset LastMovementAtUtc { get; set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
}
