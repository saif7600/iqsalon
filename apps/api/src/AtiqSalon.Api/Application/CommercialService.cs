using System.Data;
using System.Text.Json;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class CommercialService(AppDbContext db, TenantContext tenant, CommissionService commissions)
{
    public async Task<CommercialResult> CreateSale(CreateSaleRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || !tenant.CanAccessBranch(request.BranchId))
            return CommercialResult.Fail("unauthorized", "Branch access is required.");
        if (request.Lines.Count == 0) return CommercialResult.Fail("validation", "At least one line is required.");
        var branch = await db.Branches.SingleOrDefaultAsync(x =>
            x.Id == request.BranchId && x.OrganizationId == request.OrganizationId && x.IsActive, ct);
        if (branch is null) return CommercialResult.Fail("scope", "Branch is unavailable.");
        if (request.AppointmentId is { } appointmentId &&
            await db.Sales.AnyAsync(x => x.AppointmentId == appointmentId && x.Status != "Voided", ct))
            return CommercialResult.Fail("duplicate_checkout", "The appointment already has a sale.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await Lock(request.OrganizationId, ct);
        var settings = await Settings(request.OrganizationId, ct);
        var sale = new Sale
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            SaleNumber = $"SAL-{DateTimeOffset.UtcNow.Year}-{settings.NextSaleSequence++:000000}",
            AppointmentId = request.AppointmentId,
            CustomerId = request.CustomerId,
            Source = request.Source,
            CurrencyCode = settings.DefaultCurrencyCode,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedByUserId = tenant.UserId
        };
        db.Sales.Add(sale);
        var sequence = 0;
        foreach (var requested in request.Lines)
        {
            var line = await BuildLine(sale, requested, ++sequence, ct);
            if (line is null) return CommercialResult.Fail("catalogue", "A selected item is unavailable.");
            db.SaleLines.Add(line);
        }
        await Recalculate(sale, request.TipAmount, ct);
        Audit(sale, "sale.created");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(sale.Id, sale.SaleNumber);
    }

    public async Task<CommercialResult> RecordPayment(Guid saleId, RecordPaymentRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "A positive amount and idempotency key are required.");
        var replay = await db.Payments.SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.PaymentNumber, true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == saleId, ct);
        if (sale is null || !tenant.CanAccessBranch(sale.BranchId) || sale.Status is "Posted" or "Voided")
            return CommercialResult.Fail("sale", "The sale cannot accept payment.");
        await Lock(sale.OrganizationId, ct);
        var method = await db.PaymentMethods.SingleOrDefaultAsync(x =>
            x.Id == request.PaymentMethodId && x.OrganizationId == sale.OrganizationId && x.IsActive, ct);
        if (method is null) return CommercialResult.Fail("method", "Payment method is unavailable.");
        var settings = await Settings(sale.OrganizationId, ct);
        if (method.RequiresTillSession && request.TillSessionId is null && settings.RequireTillSessionForCashPayments)
            return CommercialResult.Fail("till", "An open till session is required.");
        if (request.TillSessionId is { } tillId && !await db.TillSessions.AnyAsync(x =>
                x.Id == tillId && x.BranchId == sale.BranchId && x.Status == "Open", ct))
            return CommercialResult.Fail("till", "Till session is unavailable.");
        var remaining = sale.GrandTotal - sale.PaidTotal;
        if (request.Amount > remaining && (!settings.AllowOverpayment || !method.SupportsChange))
            return CommercialResult.Fail("overpayment", "The payment exceeds the remaining balance.");

        var allocated = Math.Min(request.Amount, remaining);
        var payment = new Payment
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            PaymentNumber = $"PAY-{DateTimeOffset.UtcNow.Year}-{settings.NextPaymentSequence++:000000}",
            CustomerId = sale.CustomerId,
            PaymentMethodId = method.Id,
            CurrencyCode = sale.CurrencyCode,
            Amount = request.Amount,
            Reference = request.Reference,
            IdempotencyKey = request.IdempotencyKey,
            ReceivedByUserId = tenant.UserId,
            TillSessionId = request.TillSessionId
        };
        db.Payments.Add(payment);
        db.PaymentAllocations.Add(new PaymentAllocation
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            PaymentId = payment.Id,
            SaleId = sale.Id,
            Amount = allocated
        });
        sale.PaidTotal += allocated;
        sale.BalanceDue = Math.Max(0, sale.GrandTotal - sale.PaidTotal);
        sale.ChangeGiven += request.Amount - allocated;
        sale.Status = sale.BalanceDue == 0 ? "Paid" : "PartiallyPaid";
        Audit(sale, "payment.recorded");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(payment.Id, payment.PaymentNumber);
    }

    public async Task<CommercialResult> PostSale(Guid saleId, string idempotencyKey, CancellationToken ct)
    {
        if (tenant.TenantId is null || string.IsNullOrWhiteSpace(idempotencyKey))
            return CommercialResult.Fail("validation", "An idempotency key is required.");
        var replay = await db.Sales.SingleOrDefaultAsync(x => x.PostingIdempotencyKey == idempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.SaleNumber, true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == saleId, ct);
        if (sale is null || !tenant.CanAccessBranch(sale.BranchId) || sale.Status == "Voided")
            return CommercialResult.Fail("sale", "The sale cannot be posted.");
        if (sale.Status == "Posted") return CommercialResult.Success(sale.Id, sale.SaleNumber, true);
        var settings = await Settings(sale.OrganizationId, ct);
        if (sale.BalanceDue > 0 && !settings.AllowPartialPayments)
            return CommercialResult.Fail("settlement", "The sale must be fully settled.");
        await Lock(sale.OrganizationId, ct);
        var invoice = new Invoice
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            BranchId = sale.BranchId,
            SaleId = sale.Id,
            InvoiceNumber = $"{settings.InvoicePrefix}-{DateTimeOffset.UtcNow.Year}-{settings.NextInvoiceSequence++:000000}",
            CurrencyCode = sale.CurrencyCode,
            Subtotal = sale.Subtotal,
            DiscountTotal = sale.DiscountTotal,
            TaxTotal = sale.TaxTotal,
            GrandTotal = sale.GrandTotal,
            TaxSummaryJson = JsonSerializer.Serialize(await db.SaleLines.Where(x => x.SaleId == sale.Id)
                .GroupBy(x => new { x.TaxCodeSnapshot, x.TaxRateSnapshot })
                .Select(x => new { x.Key.TaxCodeSnapshot, x.Key.TaxRateSnapshot, Taxable = x.Sum(y => y.TaxableAmount), Tax = x.Sum(y => y.TaxAmount) })
                .ToListAsync(ct))
        };
        db.Invoices.Add(invoice);
        sale.Status = "Posted";
        sale.PostedAtUtc = DateTimeOffset.UtcNow;
        sale.PostedByUserId = tenant.UserId;
        sale.PostingIdempotencyKey = idempotencyKey;
        await commissions.GenerateForPostedSale(sale, ct);
        Audit(sale, "sale.posted");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(sale.Id, sale.SaleNumber);
    }

    private async Task<SaleLine?> BuildLine(Sale sale, SaleLineRequest request, int sequence, CancellationToken ct)
    {
        if (request.Quantity <= 0 || request.DiscountAmount < 0) return null;
        string description;
        string? sku = null;
        decimal price;
        decimal cost = 0;
        Guid? taxCodeId;
        bool commissionEligible;
        if (request.LineType == "Service" && request.ServiceId is { } serviceId)
        {
            var service = await db.SalonServices.SingleOrDefaultAsync(x =>
                x.Id == serviceId && x.OrganizationId == sale.OrganizationId && x.IsActive, ct);
            if (service is null) return null;
            description = service.Name;
            price = service.BasePrice;
            taxCodeId = null;
            commissionEligible = true;
        }
        else if (request.LineType == "Product" && request.ProductId is { } productId)
        {
            var product = await db.Products.SingleOrDefaultAsync(x =>
                x.Id == productId && x.OrganizationId == sale.OrganizationId && x.IsActive && x.IsRetail, ct);
            var branchProduct = await db.BranchProducts.SingleOrDefaultAsync(x =>
                x.ProductId == productId && x.BranchId == sale.BranchId && x.IsActive && x.IsAvailableForSale, ct);
            if (product is null || branchProduct is null) return null;
            description = product.Name;
            sku = product.Sku;
            price = branchProduct.RetailPriceOverride ?? product.RetailPrice;
            cost = product.CostPrice;
            taxCodeId = branchProduct.TaxCodeOverrideId ?? product.TaxCodeId;
            commissionEligible = product.CommissionEligible;
        }
        else return null;

        var tax = taxCodeId is { } id
            ? await db.TaxCodes.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
            : await db.TaxCodes.SingleOrDefaultAsync(x => x.OrganizationId == sale.OrganizationId && x.IsDefault && x.IsActive, ct);
        var calculation = CommercialRules.CalculateLine(request.Quantity, price, request.DiscountAmount,
            tax?.RatePercentage ?? 0, tax?.IsInclusive ?? false);
        return new SaleLine
        {
            TenantId = sale.TenantId,
            OrganizationId = sale.OrganizationId,
            SaleId = sale.Id,
            LineType = request.LineType,
            ServiceId = request.ServiceId,
            ProductId = request.ProductId,
            AppointmentServiceId = request.AppointmentServiceId,
            DescriptionSnapshot = description,
            SkuSnapshot = sku,
            Quantity = request.Quantity,
            UnitPrice = price,
            GrossAmount = calculation.Gross,
            DiscountAmount = calculation.Discount,
            NetAmount = calculation.Net,
            TaxCodeSnapshot = tax?.Code ?? "OUT",
            TaxRateSnapshot = tax?.RatePercentage ?? 0,
            TaxInclusiveSnapshot = tax?.IsInclusive ?? false,
            TaxableAmount = calculation.Taxable,
            TaxAmount = calculation.Tax,
            LineTotal = calculation.Total,
            AssignedStaffMemberId = request.StaffMemberId,
            CommissionEligible = commissionEligible,
            CostSnapshot = cost,
            Sequence = sequence
        };
    }

    private async Task Recalculate(Sale sale, decimal tip, CancellationToken ct)
    {
        var lines = db.SaleLines.Local.Where(x => x.SaleId == sale.Id).ToList();
        sale.Subtotal = lines.Sum(x => x.GrossAmount);
        sale.DiscountTotal = lines.Sum(x => x.DiscountAmount);
        sale.TaxableTotal = lines.Sum(x => x.TaxableAmount);
        sale.TaxTotal = lines.Sum(x => x.TaxAmount);
        sale.TipTotal = Math.Max(0, tip);
        sale.GrandTotal = CommercialRules.Round(lines.Sum(x => x.LineTotal) + sale.TipTotal);
        sale.BalanceDue = sale.GrandTotal;
    }

    private async Task<OrganizationCommercialSettings> Settings(Guid organizationId, CancellationToken ct)
    {
        var settings = await db.OrganizationCommercialSettings.SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct);
        if (settings is not null) return settings;
        settings = new OrganizationCommercialSettings
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId
        };
        db.OrganizationCommercialSettings.Add(settings);
        return settings;
    }

    private Task Lock(Guid organizationId, CancellationToken ct) =>
        db.Database.IsNpgsql()
            ? db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"commercial:" + organizationId}))", ct)
            : Task.CompletedTask;

    private void Audit(Sale sale, string action) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = sale.TenantId,
        OrganizationId = sale.OrganizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "Sale",
        EntityId = sale.Id.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public static class CommercialRules
{
    public static LineCalculation CalculateLine(decimal quantity, decimal unitPrice, decimal discount,
        decimal taxRate, bool inclusive)
    {
        if (quantity <= 0 || unitPrice < 0 || discount < 0 || taxRate < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var gross = Round(quantity * unitPrice);
        var appliedDiscount = Math.Min(gross, Round(discount));
        var net = gross - appliedDiscount;
        var tax = inclusive && taxRate > 0
            ? Round(net - net / (1 + taxRate / 100m))
            : Round(net * taxRate / 100m);
        var taxable = inclusive ? net - tax : net;
        return new(gross, appliedDiscount, net, Round(taxable), tax, inclusive ? net : net + tax);
    }

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record LineCalculation(decimal Gross, decimal Discount, decimal Net, decimal Taxable, decimal Tax, decimal Total);
public sealed record SaleLineRequest(string LineType, Guid? ServiceId, Guid? ProductId, Guid? AppointmentServiceId,
    Guid? StaffMemberId, decimal Quantity = 1, decimal DiscountAmount = 0);
public sealed record CreateSaleRequest(Guid OrganizationId, Guid BranchId, Guid? AppointmentId, Guid? CustomerId,
    string Source, IReadOnlyList<SaleLineRequest> Lines, decimal TipAmount = 0);
public sealed record RecordPaymentRequest(Guid PaymentMethodId, decimal Amount, string IdempotencyKey,
    string? Reference = null, Guid? TillSessionId = null);
public sealed record CommercialResult(bool IsSuccess, Guid? Id, string? Number, string? Code, string? Message, bool IsReplay = false)
{
    public static CommercialResult Success(Guid id, string number, bool replay = false) => new(true, id, number, null, null, replay);
    public static CommercialResult Fail(string code, string message) => new(false, null, null, code, message);
}
