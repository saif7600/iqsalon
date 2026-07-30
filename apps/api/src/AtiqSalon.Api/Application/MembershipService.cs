using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class MembershipService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> Enroll(Guid planId, EnrollMembershipRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
            return CommercialResult.Fail("scope", "Branch access is required.");
        var existing = await db.CustomerMemberships.SingleOrDefaultAsync(x =>
            x.EnrollmentSaleId == request.SaleId, ct);
        if (existing is not null) return CommercialResult.Success(existing.Id, existing.MembershipNumber, true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var plan = await db.MembershipPlans.SingleOrDefaultAsync(x => x.Id == planId && x.IsActive, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId && x.Status == "Posted", ct);
        if (plan is null || sale is null || sale.BranchId != request.BranchId
            || sale.OrganizationId != plan.OrganizationId || sale.CustomerId != request.CustomerId
            || sale.GrandTotal < plan.RecurringPrice)
            return CommercialResult.Fail("sale", "A posted, customer-matched sale covering the membership price is required.");
        if (await db.CustomerMemberships.AnyAsync(x => x.CustomerId == request.CustomerId
            && x.MembershipPlanId == plan.Id && x.Status == "Active", ct))
            return CommercialResult.Fail("duplicate", "Customer already has an active membership for this plan.");
        var settings = await db.OrganizationCommercialSettings.SingleAsync(x =>
            x.OrganizationId == plan.OrganizationId, ct);
        var now = DateTimeOffset.UtcNow;
        var membership = new CustomerMembership
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = plan.OrganizationId,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            MembershipPlanId = plan.Id,
            EnrollmentSaleId = sale.Id,
            MembershipNumber = $"MEM-{now.Year}-{settings.NextMembershipSequence++:000000}",
            StartsAtUtc = now,
            NextBillingAtUtc = MembershipRules.NextBilling(now, plan.BillingInterval)
        };
        db.CustomerMemberships.Add(membership);
        db.MembershipLedgerEntries.Add(new MembershipLedgerEntry
        {
            TenantId = membership.TenantId,
            OrganizationId = membership.OrganizationId,
            CustomerMembershipId = membership.Id,
            SaleId = sale.Id,
            EntryType = "Credit",
            Credits = plan.IncludedCredits,
            IdempotencyKey = $"enroll:{sale.Id}",
            Reference = "Enrollment credits",
            CreatedByUserId = tenant.UserId.Value
        });
        Audit(plan.OrganizationId, "membership.enrolled", membership.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(membership.Id, membership.MembershipNumber);
    }

    public async Task<CommercialResult> Renew(Guid membershipId, RenewMembershipRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "Idempotency key is required.");
        var replay = await db.MembershipLedgerEntries.SingleOrDefaultAsync(x =>
            x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var membership = await db.CustomerMemberships.SingleOrDefaultAsync(x =>
            x.Id == membershipId && x.Status == "Active", ct);
        if (membership is null || !tenant.CanAccessBranch(membership.BranchId))
            return CommercialResult.Fail("membership", "Active membership is unavailable.");
        var plan = await db.MembershipPlans.SingleAsync(x => x.Id == membership.MembershipPlanId, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId && x.Status == "Posted", ct);
        if (sale is null || sale.CustomerId != membership.CustomerId
            || sale.OrganizationId != membership.OrganizationId || sale.GrandTotal < plan.RecurringPrice)
            return CommercialResult.Fail("sale", "A posted, customer-matched renewal sale is required.");
        if (await db.MembershipLedgerEntries.AnyAsync(x => x.SaleId == sale.Id
            && x.EntryType == "Credit", ct))
            return CommercialResult.Fail("sale", "Renewal sale has already been used.");
        var entry = new MembershipLedgerEntry
        {
            TenantId = membership.TenantId,
            OrganizationId = membership.OrganizationId,
            CustomerMembershipId = membership.Id,
            SaleId = sale.Id,
            EntryType = "Credit",
            Credits = plan.IncludedCredits,
            IdempotencyKey = request.IdempotencyKey,
            Reference = "Renewal credits",
            CreatedByUserId = tenant.UserId.Value
        };
        db.MembershipLedgerEntries.Add(entry);
        membership.NextBillingAtUtc = MembershipRules.NextBilling(
            membership.NextBillingAtUtc > DateTimeOffset.UtcNow
                ? membership.NextBillingAtUtc
                : DateTimeOffset.UtcNow,
            plan.BillingInterval);
        Audit(membership.OrganizationId, "membership.renewed", membership.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(entry.Id, membership.MembershipNumber);
    }

    public async Task<CommercialResult> Consume(Guid membershipId, ConsumeMembershipRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null || request.Credits <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "Positive credits and idempotency key are required.");
        var replay = await db.MembershipLedgerEntries.SingleOrDefaultAsync(x =>
            x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var membership = await db.CustomerMemberships.SingleOrDefaultAsync(x =>
            x.Id == membershipId && x.Status == "Active", ct);
        if (membership is null || !tenant.CanAccessBranch(membership.BranchId)
            || membership.EndsAtUtc <= DateTimeOffset.UtcNow)
            return CommercialResult.Fail("membership", "Membership is inactive, expired, or inaccessible.");
        if (request.AppointmentId is { } appointmentId && !await db.Appointments.AnyAsync(x =>
                x.Id == appointmentId && x.CustomerId == membership.CustomerId
                && x.BranchId == membership.BranchId, ct))
            return CommercialResult.Fail("appointment", "Appointment does not belong to this member and branch.");
        var balance = await db.MembershipLedgerEntries.Where(x => x.CustomerMembershipId == membership.Id)
            .SumAsync(x => x.EntryType == "Debit" ? -x.Credits : x.Credits, ct);
        if (request.Credits > balance) return CommercialResult.Fail("balance", "Membership credits are insufficient.");
        var entry = new MembershipLedgerEntry
        {
            TenantId = membership.TenantId,
            OrganizationId = membership.OrganizationId,
            CustomerMembershipId = membership.Id,
            AppointmentId = request.AppointmentId,
            EntryType = "Debit",
            Credits = request.Credits,
            IdempotencyKey = request.IdempotencyKey,
            Reference = request.Reference?.Trim(),
            CreatedByUserId = tenant.UserId.Value
        };
        db.MembershipLedgerEntries.Add(entry);
        Audit(membership.OrganizationId, "membership.consumed", membership.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(entry.Id, membership.MembershipNumber);
    }

    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "Membership",
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public static class MembershipRules
{
    public static DateTimeOffset NextBilling(DateTimeOffset start, string interval) => interval switch
    {
        "Weekly" => start.AddDays(7),
        "Quarterly" => start.AddMonths(3),
        "Annual" => start.AddYears(1),
        _ => start.AddMonths(1)
    };
}

public sealed record EnrollMembershipRequest(Guid BranchId, Guid CustomerId, Guid SaleId);
public sealed record RenewMembershipRequest(Guid SaleId, string IdempotencyKey);
public sealed record ConsumeMembershipRequest(decimal Credits, string IdempotencyKey,
    Guid? AppointmentId = null, string? Reference = null);
