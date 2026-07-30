using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class TransferService(AppDbContext db, TenantContext tenant, InventoryService inventory)
{
    public async Task<CommercialResult> Create(CreateTransferRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.SourceBranchId)
            || request.SourceBranchId == request.DestinationBranchId || request.Lines.Count == 0
            || request.Lines.Any(x => x.Quantity <= 0))
            return CommercialResult.Fail("validation", "Distinct branches and positive lines are required.");
        var locations = await db.InventoryLocations
            .Where(x => x.Id == request.SourceLocationId || x.Id == request.DestinationLocationId).ToListAsync(ct);
        if (!locations.Any(x => x.Id == request.SourceLocationId && x.BranchId == request.SourceBranchId)
            || !locations.Any(x => x.Id == request.DestinationLocationId && x.BranchId == request.DestinationBranchId))
            return CommercialResult.Fail("location", "Locations must belong to their branches.");
        var transfer = new StockTransfer
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            SourceBranchId = request.SourceBranchId,
            SourceLocationId = request.SourceLocationId,
            DestinationBranchId = request.DestinationBranchId,
            DestinationLocationId = request.DestinationLocationId,
            TransferNumber = $"TRF-{await db.StockTransfers.CountAsync(x => x.OrganizationId == request.OrganizationId, ct) + 1:000000}",
            Notes = request.Notes?.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        db.StockTransfers.Add(transfer);
        db.StockTransferLines.AddRange(request.Lines.Select((x, index) => new StockTransferLine
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            StockTransferId = transfer.Id,
            ProductId = x.ProductId,
            BatchId = x.BatchId,
            Sequence = index + 1,
            RequestedQuantity = x.Quantity
        }));
        await db.SaveChangesAsync(ct);
        return CommercialResult.Success(transfer.Id, transfer.TransferNumber);
    }

    public async Task<CommercialResult> Transition(Guid id, string action, CancellationToken ct)
    {
        var transfer = await db.StockTransfers.SingleOrDefaultAsync(x => x.Id == id, ct);
        var expected = action switch { "approve" => "Draft", "dispatch" => "Approved", "receive" => "Dispatched", _ => "" };
        if (transfer is null || tenant.UserId is null || transfer.Status != expected)
            return CommercialResult.Fail("status", "Transfer or transition is invalid.");
        var branchId = action == "receive" ? transfer.DestinationBranchId : transfer.SourceBranchId;
        if (!tenant.CanAccessBranch(branchId)) return CommercialResult.Fail("branch", "Branch access is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var lines = await db.StockTransferLines.Where(x => x.StockTransferId == transfer.Id).ToListAsync(ct);
        if (action is "dispatch" or "receive")
            foreach (var line in lines)
            {
                var inbound = action == "receive";
                var quantity = inbound ? line.DispatchedQuantity : line.RequestedQuantity;
                var result = await inventory.PostMovement(new PostStockMovementRequest(
                    transfer.OrganizationId, branchId,
                    inbound ? transfer.DestinationLocationId : transfer.SourceLocationId,
                    line.ProductId, line.BatchId, inbound ? "TransferReceipt" : "TransferDispatch",
                    inbound ? "Inbound" : "Outbound", quantity, inbound ? line.UnitCost : null,
                    "StockTransfer", transfer.Id, transfer.TransferNumber,
                    inbound ? "TransferIn" : "TransferOut", transfer.Notes,
                    $"transfer:{transfer.Id:N}:{action}:{line.Id:N}"), ct);
                if (!result.IsSuccess || result.Id is null) return result;
                var movement = await db.StockMovements.SingleAsync(x => x.Id == result.Id, ct);
                if (inbound) line.ReceivedQuantity = quantity;
                else { line.DispatchedQuantity = quantity; line.UnitCost = movement.UnitCost; }
            }
        if (action == "approve") { transfer.Status = "Approved"; transfer.ApprovedByUserId = tenant.UserId; }
        if (action == "dispatch") { transfer.Status = "Dispatched"; transfer.DispatchedByUserId = tenant.UserId; transfer.DispatchedAtUtc = DateTimeOffset.UtcNow; }
        if (action == "receive") { transfer.Status = "Received"; transfer.ReceivedByUserId = tenant.UserId; transfer.ReceivedAtUtc = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(transfer.Id, transfer.TransferNumber);
    }
}

public sealed record TransferLineRequest(Guid ProductId, Guid? BatchId, decimal Quantity);
public sealed record CreateTransferRequest(Guid OrganizationId, Guid SourceBranchId, Guid SourceLocationId,
    Guid DestinationBranchId, Guid DestinationLocationId, string? Notes,
    IReadOnlyCollection<TransferLineRequest> Lines);
