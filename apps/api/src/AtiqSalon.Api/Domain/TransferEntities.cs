namespace AtiqSalon.Api.Domain;

public sealed class StockTransfer : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SourceBranchId { get; set; }
    public Guid SourceLocationId { get; set; }
    public Guid DestinationBranchId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public string TransferNumber { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? DispatchedByUserId { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
}

public sealed class StockTransferLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public int Sequence { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
}
