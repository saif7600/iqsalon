using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class PurchasingService(AppDbContext db, TenantContext tenant, InventoryService inventory)
{
    public async Task<CommercialResult> CreateOrder(CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Lines.Count == 0 || request.Lines.Any(x => x.Quantity <= 0 || x.UnitCost < 0 || x.ConversionFactorToBase <= 0))
            return CommercialResult.Fail("validation", "A permitted branch and positive order lines are required.");
        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.OrganizationId == request.OrganizationId && x.Status == "Active", ct))
            return CommercialResult.Fail("supplier", "Supplier is unavailable.");
        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToArray();
        if (await db.Products.CountAsync(x => productIds.Contains(x.Id) && x.OrganizationId == request.OrganizationId && x.IsActive, ct) != productIds.Length)
            return CommercialResult.Fail("product", "One or more products are unavailable.");
        var count = await db.PurchaseOrders.CountAsync(x => x.OrganizationId == request.OrganizationId, ct);
        var order = new PurchaseOrder
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            SupplierId = request.SupplierId,
            PurchaseOrderNumber = $"PO-{count + 1:000000}",
            OrderDate = request.OrderDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            Notes = request.Notes?.Trim(),
            SupplierReference = request.SupplierReference?.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        var lines = request.Lines.Select((line, index) =>
        {
            var subtotal = InventoryRules.RoundCost(line.Quantity * line.UnitCost, 4);
            var tax = InventoryRules.RoundCost(subtotal * line.TaxRate / 100m, 4);
            return new PurchaseOrderLine
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                PurchaseOrderId = order.Id,
                ProductId = line.ProductId,
                UnitOfMeasureId = line.UnitOfMeasureId,
                Sequence = index + 1,
                OrderedQuantity = line.Quantity,
                ConversionFactorToBase = line.ConversionFactorToBase,
                UnitCost = line.UnitCost,
                TaxRate = line.TaxRate,
                LineSubtotal = subtotal,
                LineTax = tax,
                LineTotal = subtotal + tax
            };
        }).ToArray();
        order.Subtotal = lines.Sum(x => x.LineSubtotal); order.TaxTotal = lines.Sum(x => x.LineTax); order.Total = lines.Sum(x => x.LineTotal);
        db.PurchaseOrders.Add(order); db.PurchaseOrderLines.AddRange(lines);
        Audit(request.OrganizationId, "purchase_order.created", "PurchaseOrder", order.Id);
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(order.Id, order.PurchaseOrderNumber);
    }

    public async Task<CommercialResult> ApproveOrder(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null || !tenant.CanAccessBranch(order.BranchId)) return CommercialResult.Fail("not_found", "Purchase order not found.");
        if (order.Status != "Draft") return CommercialResult.Fail("status", "Only draft purchase orders can be approved.");
        order.Status = "Approved"; order.ApprovedByUserId = tenant.UserId; order.ApprovedAtUtc = DateTimeOffset.UtcNow;
        Audit(order.OrganizationId, "purchase_order.approved", "PurchaseOrder", order.Id);
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(order.Id, order.PurchaseOrderNumber);
    }

    public async Task<CommercialResult> PostReceipt(PostGoodsReceiptRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Lines.Count == 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "A permitted branch, lines, and idempotency key are required.");
        var replay = await db.GoodsReceipts.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.ReceiptNumber, true);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == request.PurchaseOrderId
            && x.OrganizationId == request.OrganizationId && x.BranchId == request.BranchId, ct);
        if (order is null || order.Status is not ("Approved" or "PartiallyReceived"))
            return CommercialResult.Fail("status", "An approved purchase order is required.");
        if (!await db.InventoryLocations.AnyAsync(x => x.Id == request.InventoryLocationId && x.BranchId == request.BranchId && x.IsActive, ct))
            return CommercialResult.Fail("location", "Inventory location is unavailable.");
        var orderLines = await db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == order.Id).ToDictionaryAsync(x => x.Id, ct);
        if (!PurchasingRules.CanReceive(request.Lines, orderLines))
            return CommercialResult.Fail("quantity", "Receipt lines must match the order and cannot exceed outstanding quantities.");
        var count = await db.GoodsReceipts.CountAsync(x => x.OrganizationId == request.OrganizationId, ct);
        var receipt = new GoodsReceipt
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            PurchaseOrderId = order.Id,
            SupplierId = order.SupplierId,
            InventoryLocationId = request.InventoryLocationId,
            ReceiptNumber = $"GRN-{count + 1:000000}",
            ReceiptDate = request.ReceiptDate,
            Status = "Posted",
            SupplierDeliveryNote = request.SupplierDeliveryNote?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = tenant.UserId.Value,
            PostedByUserId = tenant.UserId.Value,
            PostedAtUtc = DateTimeOffset.UtcNow,
            IdempotencyKey = request.IdempotencyKey.Trim()
        };
        db.GoodsReceipts.Add(receipt);
        foreach (var input in request.Lines)
        {
            var orderLine = orderLines[input.PurchaseOrderLineId];
            var baseQuantity = input.Quantity * orderLine.ConversionFactorToBase;
            db.GoodsReceiptLines.Add(new GoodsReceiptLine
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = request.OrganizationId,
                GoodsReceiptId = receipt.Id,
                PurchaseOrderLineId = orderLine.Id,
                ProductId = orderLine.ProductId,
                BatchId = input.BatchId,
                Sequence = input.Sequence,
                ReceivedQuantity = input.Quantity,
                QuantityBaseUnit = baseQuantity,
                UnitCost = orderLine.UnitCost
            });
            var movement = await inventory.PostMovement(new PostStockMovementRequest(request.OrganizationId, request.BranchId,
                request.InventoryLocationId, orderLine.ProductId, input.BatchId, "GoodsReceipt", "Inbound", baseQuantity,
                orderLine.UnitCost / orderLine.ConversionFactorToBase, "GoodsReceipt", receipt.Id, receipt.ReceiptNumber,
                "PurchaseReceipt", request.Notes, $"{request.IdempotencyKey}:{orderLine.Id:N}"), ct);
            if (!movement.IsSuccess) return movement;
            orderLine.ReceivedQuantity += input.Quantity;
        }
        order.Status = orderLines.Values.All(x => x.ReceivedQuantity >= x.OrderedQuantity) ? "Received" : "PartiallyReceived";
        Audit(request.OrganizationId, "goods_receipt.posted", "GoodsReceipt", receipt.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(receipt.Id, receipt.ReceiptNumber);
    }

    private void Audit(Guid organizationId, string action, string type, Guid id) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = type,
        EntityId = id.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public static class PurchasingRules
{
    public static bool CanReceive(IReadOnlyCollection<GoodsReceiptLineRequest> receiptLines,
        IReadOnlyDictionary<Guid, PurchaseOrderLine> orderLines) =>
        receiptLines.Count > 0 && receiptLines.All(x => x.Quantity > 0 && orderLines.TryGetValue(x.PurchaseOrderLineId, out var line)
            && x.Quantity <= line.OrderedQuantity - line.ReceivedQuantity)
        && receiptLines.GroupBy(x => x.PurchaseOrderLineId).All(x => x.Count() == 1);
}

public sealed record PurchaseOrderLineRequest(Guid ProductId, Guid UnitOfMeasureId, decimal Quantity,
    decimal ConversionFactorToBase, decimal UnitCost, decimal TaxRate);
public sealed record CreatePurchaseOrderRequest(Guid OrganizationId, Guid BranchId, Guid SupplierId,
    DateOnly OrderDate, DateOnly? ExpectedDeliveryDate, string CurrencyCode, string? SupplierReference,
    string? Notes, IReadOnlyCollection<PurchaseOrderLineRequest> Lines);
public sealed record GoodsReceiptLineRequest(Guid PurchaseOrderLineId, Guid? BatchId, int Sequence, decimal Quantity);
public sealed record PostGoodsReceiptRequest(Guid OrganizationId, Guid BranchId, Guid PurchaseOrderId,
    Guid InventoryLocationId, DateOnly ReceiptDate, string? SupplierDeliveryNote, string? Notes,
    string IdempotencyKey, IReadOnlyCollection<GoodsReceiptLineRequest> Lines);
