using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class PackageService(AppDbContext db, TenantContext tenant)
{
    public async Task<CommercialResult> Activate(Guid definitionId, ActivatePackageRequest request, CancellationToken ct)
    {
        if (tenant.TenantId is null || tenant.UserId is null || !tenant.CanAccessBranch(request.BranchId))
            return CommercialResult.Fail("scope", "Branch access is required.");
        var existing = await db.CustomerPackages.SingleOrDefaultAsync(x => x.SaleId == request.SaleId, ct);
        if (existing is not null) return CommercialResult.Success(existing.Id, existing.PackageNumber, true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var definition = await db.PackageDefinitions.SingleOrDefaultAsync(x => x.Id == definitionId && x.IsActive, ct);
        var sale = await db.Sales.SingleOrDefaultAsync(x => x.Id == request.SaleId && x.Status == "Posted", ct);
        if (definition is null || sale is null || sale.BranchId != request.BranchId
            || sale.OrganizationId != definition.OrganizationId || sale.CustomerId is null
            || sale.CustomerId != request.CustomerId || sale.GrandTotal < definition.Price)
            return CommercialResult.Fail("sale", "A posted, customer-matched sale covering the package price is required.");
        var entitlements = await db.PackageEntitlements.Where(x =>
            x.PackageDefinitionId == definition.Id && x.Quantity > 0).ToListAsync(ct);
        if (entitlements.Count == 0) return CommercialResult.Fail("entitlements", "Package has no service entitlements.");
        var settings = await db.OrganizationCommercialSettings.SingleAsync(x =>
            x.OrganizationId == definition.OrganizationId, ct);
        var customerPackage = new CustomerPackage
        {
            TenantId = tenant.TenantId.Value,
            OrganizationId = definition.OrganizationId,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            PackageDefinitionId = definition.Id,
            SaleId = sale.Id,
            PackageNumber = $"PKG-{DateTimeOffset.UtcNow.Year}-{settings.NextPackageSequence++:000000}",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(definition.ValidityDays)
        };
        db.CustomerPackages.Add(customerPackage);
        db.PackageLedgerEntries.AddRange(entitlements.Select(entitlement => new PackageLedgerEntry
        {
            TenantId = customerPackage.TenantId,
            OrganizationId = customerPackage.OrganizationId,
            CustomerPackageId = customerPackage.Id,
            ServiceId = entitlement.ServiceId,
            SaleId = sale.Id,
            EntryType = "Credit",
            Quantity = entitlement.Quantity,
            IdempotencyKey = $"activate:{sale.Id}:{entitlement.ServiceId}",
            CreatedByUserId = tenant.UserId.Value
        }));
        Audit(definition.OrganizationId, "package.activated", customerPackage.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(customerPackage.Id, customerPackage.PackageNumber);
    }

    public async Task<CommercialResult> Consume(Guid customerPackageId, ConsumePackageRequest request, CancellationToken ct)
    {
        if (tenant.UserId is null || request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return CommercialResult.Fail("validation", "Positive quantity and idempotency key are required.");
        var replay = await db.PackageLedgerEntries.SingleOrDefaultAsync(x =>
            x.IdempotencyKey == request.IdempotencyKey, ct);
        if (replay is not null) return CommercialResult.Success(replay.Id, replay.Id.ToString(), true);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var package = await db.CustomerPackages.SingleOrDefaultAsync(x =>
            x.Id == customerPackageId && x.Status == "Active", ct);
        if (package is null || package.ExpiresAtUtc <= DateTimeOffset.UtcNow
            || !tenant.CanAccessBranch(package.BranchId))
            return CommercialResult.Fail("package", "Package is inactive, expired, or inaccessible.");
        if (request.AppointmentId is { } appointmentId && !await db.Appointments.AnyAsync(x =>
                x.Id == appointmentId && x.CustomerId == package.CustomerId && x.BranchId == package.BranchId, ct))
            return CommercialResult.Fail("appointment", "Appointment does not belong to the package customer and branch.");
        var balance = await db.PackageLedgerEntries.Where(x =>
                x.CustomerPackageId == package.Id && x.ServiceId == request.ServiceId)
            .SumAsync(x => x.EntryType == "Debit" ? -x.Quantity : x.Quantity, ct);
        if (request.Quantity > balance) return CommercialResult.Fail("balance", "Package entitlement is insufficient.");
        var entry = new PackageLedgerEntry
        {
            TenantId = package.TenantId,
            OrganizationId = package.OrganizationId,
            CustomerPackageId = package.Id,
            ServiceId = request.ServiceId,
            AppointmentId = request.AppointmentId,
            EntryType = "Debit",
            Quantity = request.Quantity,
            IdempotencyKey = request.IdempotencyKey,
            CreatedByUserId = tenant.UserId.Value
        };
        db.PackageLedgerEntries.Add(entry);
        Audit(package.OrganizationId, "package.consumed", package.Id);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CommercialResult.Success(entry.Id, package.PackageNumber);
    }

    private void Audit(Guid organizationId, string action, Guid entityId) => db.AuditEvents.Add(new AuditEvent
    {
        TenantId = tenant.TenantId!.Value,
        OrganizationId = organizationId,
        ActorUserId = tenant.UserId,
        Action = action,
        EntityType = "Package",
        EntityId = entityId.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    });
}

public sealed record ActivatePackageRequest(Guid BranchId, Guid CustomerId, Guid SaleId);
public sealed record ConsumePackageRequest(Guid ServiceId, decimal Quantity, string IdempotencyKey,
    Guid? AppointmentId = null);
