using AtiqSalon.Api.Application;
using AtiqSalon.Api.Domain;

namespace AtiqSalon.Api.Tests;

public sealed class PurchasingRulesTests
{
    [Fact]
    public void Rejects_over_receipt()
    {
        var line = new PurchaseOrderLine { Id = Guid.NewGuid(), OrderedQuantity = 10, ReceivedQuantity = 4 };
        Assert.False(PurchasingRules.CanReceive(
            [new GoodsReceiptLineRequest(line.Id, null, 1, 7)],
            new Dictionary<Guid, PurchaseOrderLine> { [line.Id] = line }));
    }

    [Fact]
    public void Accepts_exact_outstanding_quantity()
    {
        var line = new PurchaseOrderLine { Id = Guid.NewGuid(), OrderedQuantity = 10, ReceivedQuantity = 4 };
        Assert.True(PurchasingRules.CanReceive(
            [new GoodsReceiptLineRequest(line.Id, null, 1, 6)],
            new Dictionary<Guid, PurchaseOrderLine> { [line.Id] = line }));
    }
}
