namespace AtiqSalon.Api.Domain;

public sealed class ServiceRecipe : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ServiceId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ActivatedByUserId { get; set; }
}

public sealed class ServiceRecipeLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ServiceRecipeId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityBaseUnit { get; set; }
    public decimal WastageAllowancePercent { get; set; }
    public int Sequence { get; set; }
}

public sealed class AppointmentConsumption : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = "Posted";
    public string IdempotencyKey { get; set; } = "";
    public Guid PostedByUserId { get; set; }
    public DateTimeOffset PostedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ReversedByUserId { get; set; }
    public DateTimeOffset? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
}

public sealed class AppointmentConsumptionLine : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid AppointmentConsumptionId { get; set; }
    public Guid AppointmentServiceId { get; set; }
    public Guid ServiceRecipeLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal QuantityBaseUnit { get; set; }
    public decimal UnitCost { get; set; }
    public Guid StockMovementId { get; set; }
}
