using System.Data;
using System.Security.Cryptography;
using System.Text;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class GiftCardService(AppDbContext db, TenantContext tenant)
{
    public async Task<GiftCardIssueResult> Issue(IssueGiftCardRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId)
            || request.Value <= 0)
            return GiftCardIssueResult.Fail("Branch access and positive value are required.");
        var existing = await db.GiftCards.SingleOrDefaultAsync(x => x.IssuanceSaleId == request.SaleId, ct);
        if (existing is not null)
            return GiftCardIssueResult.Fail("Issuance sale has already been used. The original code cannot be recovered.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId && x.Status == "Posted", ct);
        if (sale is null || sale.BranchId != request.BranchId || sale.OrganizationId != request.OrganizationId
            || sale.GrandTotal < request.Value || sale.CurrencyCode != request.CurrencyCode)
            return GiftCardIssueResult.Fail("A posted sale covering the gift-card value and currency is required.");
        var settings = await db.OrganizationCommercialSettings.SingleAsync(x =>
            x.OrganizationId == request.OrganizationId, ct);
        var plainCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var card = new GiftCard
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            IssuanceSaleId = sale.Id,
            GiftCardNumber = $"GFT-{DateTimeOffset.UtcNow.Year}-{settings.NextGiftCardSequence++:000000}",
            CodeHash = Hash(plainCode),
            CodeLastFour = plainCode[^4..],
            CurrencyCode = request.CurrencyCode,
            InitialValue = CommercialRules.Round(request.Value),
            CustomerId = request.CustomerId,
            ExpiresAtUtc = request.ExpiresAtUtc
        };
        db.GiftCards.Add(card);
        db.GiftCardLedgerEntries.Add(new GiftCardLedgerEntry
        {
            TenantId = card.TenantId,
            OrganizationId = card.OrganizationId,
            GiftCardId = card.Id,
            SaleId = sale.Id,
            EntryType = "Issue",
            Amount = card.InitialValue,
            IdempotencyKey = $"issue:{sale.Id}",
            CreatedByUserId = tenant.UserId.Value
        });
        Audit(card.OrganizationId, "gift_card.issued", card.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return GiftCardIssueResult.Success(card.Id, card.GiftCardNumber, plainCode, card.CodeLastFour);
    }

    public async Task<CommercialResult> Redeem(RedeemGiftCardRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || request.Amount <= 0
            || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "Code, positive amount, and idempotency key are required.");
        var replay = await db.GiftCardLedgerEntries.SingleOrDefaultAsync(x =>
            x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var hash = Hash(request.Code.Trim().ToUpperInvariant());
        var card = await db.GiftCards.SingleOrDefaultAsync(x => x.CodeHash == hash && x.Status == "Active", ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId
            && x.Status != "Posted" && x.Status != "Voided", ct);
        if (card is null || sale is null || card.OrganizationId != sale.OrganizationId
            || card.CurrencyCode != sale.CurrencyCode || !tenant.CanAccessBranch(sale.BranchId)
            || card.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            return CommercialResult.Fail("gift_card", "Gift card or sale is unavailable, expired, or currency-mismatched.");
        var method = await db.PaymentMethods.SingleOrDefaultAsync(x =>
            x.OrganizationId == card.OrganizationId && x.Type == "GiftCard" && x.IsActive, ct);
        if (method is null) return CommercialResult.Fail("method", "An active GiftCard payment method is required.");
        var balance = await Balance(card.Id, ct);
        var applied = CommercialRules.Round(Math.Min(request.Amount, Math.Min(balance, sale.BalanceDue)));
        if (applied <= 0) return CommercialResult.Fail("balance", "Gift card has no applicable balance.");
        var settings = await db.OrganizationCommercialSettings.SingleAsync(x =>
            x.OrganizationId == card.OrganizationId, ct);
        var payment = new Payment
        {
            TenantId = card.TenantId,
            OrganizationId = card.OrganizationId,
            BranchId = sale.BranchId,
            PaymentNumber = $"PAY-{DateTimeOffset.UtcNow.Year}-{settings.NextPaymentSequence++:000000}",
            CustomerId = sale.CustomerId,
            PaymentMethodId = method.Id,
            Amount = applied,
            CurrencyCode = sale.CurrencyCode,
            Provider = "StoredValue",
            Reference = card.GiftCardNumber,
            IdempotencyKey = $"gift-card:{request.IdempotencyKey}",
            ReceivedByUserId = tenant.UserId
        };
        var entry = new GiftCardLedgerEntry
        {
            TenantId = card.TenantId,
            OrganizationId = card.OrganizationId,
            GiftCardId = card.Id,
            SaleId = sale.Id,
            PaymentId = payment.Id,
            EntryType = "Redeem",
            Amount = applied,
            IdempotencyKey = request.IdempotencyKey,
            CreatedByUserId = tenant.UserId.Value
        };
        db.AddRange(payment, entry, new PaymentAllocation
        {
            TenantId = card.TenantId,
            OrganizationId = card.OrganizationId,
            PaymentId = payment.Id,
            SaleId = sale.Id,
            Amount = applied
        });
        sale.PaidTotal = CommercialRules.Round(sale.PaidTotal + applied);
        sale.BalanceDue = CommercialRules.Round(Math.Max(0, sale.GrandTotal - sale.PaidTotal));
        sale.Status = sale.BalanceDue == 0 ? "Paid" : "PartiallyPaid";
        if (applied == balance) card.Status = "Consumed";
        Audit(card.OrganizationId, "gift_card.redeemed", card.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(entry.Id, card.GiftCardNumber);
    }

    public async Task<GiftCardBalanceResult?> GetBalance(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var card = await db.GiftCards.SingleOrDefaultAsync(x => x.CodeHash == Hash(code.Trim().ToUpperInvariant()), ct);
        if (card is null) return null;
        var balance = await Balance(card.Id, ct);
        var status = card.ExpiresAtUtc <= DateTimeOffset.UtcNow ? "Expired" : card.Status;
        return new(card.GiftCardNumber, card.CodeLastFour, card.CurrencyCode, balance, status, card.ExpiresAtUtc);
    }

    private async Task<decimal> Balance(Guid cardId, CancellationToken ct) =>
        CommercialRules.Round(await db.GiftCardLedgerEntries.Where(x => x.GiftCardId == cardId)
            .SumAsync(x => x.EntryType == "Redeem" ? -x.Amount : x.Amount, ct));
    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "GiftCard",
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public sealed record IssueGiftCardRequest(Guid OrganizationId, Guid BranchId, Guid SaleId,
    decimal Value, string CurrencyCode = "AED", Guid? CustomerId = null, DateTimeOffset? ExpiresAtUtc = null);
public sealed record RedeemGiftCardRequest(string Code, Guid SaleId, decimal Amount, string IdempotencyKey);
public sealed record GiftCardBalanceResult(string Number, string LastFour, string CurrencyCode,
    decimal Balance, string Status, DateTimeOffset? ExpiresAtUtc);
public sealed record GiftCardIssueResult(bool IsSuccess, Guid? Id, string? Number, string? Code,
    string? LastFour, string? Message)
{
    public static GiftCardIssueResult Success(Guid id, string number, string code, string lastFour) =>
        new(true, id, number, code, lastFour, null);
    public static GiftCardIssueResult Fail(string message) => new(false, null, null, null, null, message);
}
