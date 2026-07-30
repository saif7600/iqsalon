using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class ManagementEndpoints
{
    public static IEndpointRouteBuilder MapManagementApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        api.MapGet("/staff/{id:guid}/operating-profile", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (staff is null) return Results.NotFound();
            var assignments = await db.StaffBranchAssignments.Where(x => x.StaffMemberId == id).OrderByDescending(x => x.IsPrimary).ToListAsync(ct);
            if (!tenant.HasOrganizationWideAccess && !assignments.Any(x => tenant.BranchIds.Contains(x.BranchId))) return Results.NotFound();
            return Results.Ok(new
            {
                staff,
                assignments,
                capabilities = await db.StaffServiceCapabilities.Where(x => x.StaffMemberId == id).ToListAsync(ct),
                workingHours = await db.StaffWorkingHours.Where(x => x.StaffMemberId == id).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).ToListAsync(ct),
                breaks = await db.StaffBreakRules.Where(x => x.StaffMemberId == id).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).ToListAsync(ct),
                overrides = await db.StaffAvailabilityOverrides.Where(x => x.StaffMemberId == id && x.EndsAtUtc >= DateTimeOffset.UtcNow).OrderBy(x => x.StartsAtUtc).ToListAsync(ct)
            });
        }).RequireAuthorization("staff.read");

        api.MapPut("/staff/{id:guid}/configuration", async (Guid id, StaffConfigurationRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null) return Results.Unauthorized();
            var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (staff is null) return Results.NotFound();
            var branchIds = request.Assignments.Select(x => x.BranchId)
                .Concat(request.WorkingHours.Select(x => x.BranchId))
                .Concat(request.Breaks.Select(x => x.BranchId)).Distinct().ToArray();
            if (!tenant.HasOrganizationWideAccess && branchIds.Any(x => !tenant.CanAccessBranch(x))) return Results.Forbid();
            var branches = await db.Branches.Where(x => branchIds.Contains(x.Id) && x.OrganizationId == staff.OrganizationId && x.IsActive).Select(x => x.Id).ToListAsync(ct);
            if (branches.Count != branchIds.Length) return Invalid("branches", "Every branch must belong to the staff organization.");
            var serviceIds = request.Capabilities.Select(x => x.ServiceId).Distinct().ToArray();
            if (await db.SalonServices.CountAsync(x => serviceIds.Contains(x.Id) && x.OrganizationId == staff.OrganizationId && x.IsActive, ct) != serviceIds.Length)
                return Invalid("services", "Every service must belong to the staff organization.");
            if (request.WorkingHours.Any(x => x.StartTime >= x.EndTime) || request.Breaks.Any(x => x.StartTime >= x.EndTime))
                return Invalid("schedule", "Schedule start times must be before end times.");
            var assignedBranches = request.Assignments.Select(x => x.BranchId).ToHashSet();
            if (request.Capabilities.Any(x => x.BranchId.HasValue && !assignedBranches.Contains(x.BranchId.Value))
                || request.WorkingHours.Any(x => !assignedBranches.Contains(x.BranchId))
                || request.Breaks.Any(x => !assignedBranches.Contains(x.BranchId)))
                return Invalid("assignments", "Capabilities and schedules require an active branch assignment.");

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.StaffBranchAssignments.RemoveRange(await db.StaffBranchAssignments.Where(x => x.StaffMemberId == id).ToListAsync(ct));
            db.StaffServiceCapabilities.RemoveRange(await db.StaffServiceCapabilities.Where(x => x.StaffMemberId == id).ToListAsync(ct));
            db.StaffWorkingHours.RemoveRange(await db.StaffWorkingHours.Where(x => x.StaffMemberId == id).ToListAsync(ct));
            db.StaffBreakRules.RemoveRange(await db.StaffBreakRules.Where(x => x.StaffMemberId == id).ToListAsync(ct));
            db.StaffBranchAssignments.AddRange(request.Assignments.Select(x => new StaffBranchAssignment
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = staff.OrganizationId,
                StaffMemberId = id,
                BranchId = x.BranchId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsPrimary = x.IsPrimary,
                IsActive = true
            }));
            db.StaffServiceCapabilities.AddRange(request.Capabilities.Select(x => new StaffServiceCapability
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = staff.OrganizationId,
                StaffMemberId = id,
                ServiceId = x.ServiceId,
                BranchId = x.BranchId,
                CanPerform = true,
                OnlineBookingEnabled = x.OnlineBookingEnabled,
                SkillLevel = x.SkillLevel,
                PriceOverride = x.PriceOverride,
                DurationOverrideMinutes = x.DurationOverrideMinutes
            }));
            db.StaffWorkingHours.AddRange(request.WorkingHours.Select(x => new StaffWorkingHours
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = staff.OrganizationId,
                StaffMemberId = id,
                BranchId = x.BranchId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo
            }));
            db.StaffBreakRules.AddRange(request.Breaks.Select(x => new StaffBreakRule
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = staff.OrganizationId,
                StaffMemberId = id,
                BranchId = x.BranchId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo
            }));
            Audit(db, tenant, staff.OrganizationId, "staff.configuration_changed", id);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("staff.schedule.manage");

        api.MapPost("/staff/{id:guid}/availability-overrides", async (Guid id, AvailabilityOverrideRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || !tenant.CanAccessBranch(request.BranchId)) return Results.Forbid();
            var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (staff is null || request.StartsAtUtc >= request.EndsAtUtc) return Invalid("interval", "A valid availability interval is required.");
            if (!await db.StaffBranchAssignments.AnyAsync(x => x.StaffMemberId == id && x.BranchId == request.BranchId && x.IsActive, ct))
                return Invalid("branchId", "Staff must be assigned to this branch.");
            var item = new StaffAvailabilityOverride
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = staff.OrganizationId,
                StaffMemberId = id,
                BranchId = request.BranchId,
                StartsAtUtc = request.StartsAtUtc,
                EndsAtUtc = request.EndsAtUtc,
                OverrideType = request.OverrideType,
                Reason = request.Reason
            };
            db.StaffAvailabilityOverrides.Add(item);
            Audit(db, tenant, staff.OrganizationId, "staff.availability_override_created", item.Id);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/staff/{id}/availability-overrides/{item.Id}", item);
        }).RequireAuthorization("staff.schedule.manage");

        api.MapGet("/customers/{id:guid}/crm-profile", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (customer is null) return Results.NotFound();
            var notes = db.CustomerNotes.Where(x => x.CustomerId == id);
            if (!tenant.HasPermission("customers.notes.sensitive.read")) notes = notes.Where(x => !x.IsSensitive);
            return Results.Ok(new
            {
                customer,
                notes = await notes.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct),
                appointments = await db.Appointments.Where(x => x.CustomerId == id && (tenant.HasOrganizationWideAccess || tenant.BranchIds.Contains(x.BranchId))).OrderByDescending(x => x.StartAtUtc).Take(50).ToListAsync(ct)
            });
        }).RequireAuthorization("customers.read");

        api.MapPost("/customers/{id:guid}/notes", async (Guid id, CustomerNoteRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null || tenant.UserId is null) return Results.Unauthorized();
            if (request.IsSensitive && !tenant.HasPermission("customers.notes.sensitive.read")) return Results.Forbid();
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (customer is null || string.IsNullOrWhiteSpace(request.Content)) return Invalid("content", "Note content is required.");
            var note = new CustomerNote
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = customer.OrganizationId,
                CustomerId = id,
                AuthorUserId = tenant.UserId.Value,
                NoteType = request.NoteType,
                Content = request.Content.Trim(),
                IsSensitive = request.IsSensitive
            };
            db.CustomerNotes.Add(note);
            Audit(db, tenant, customer.OrganizationId, "customer.note_created", note.Id);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/customers/{id}/notes/{note.Id}", note);
        }).RequireAuthorization("customers.notes.create");

        api.MapPut("/customers/{id:guid}/consent", async (Guid id, CustomerConsentRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (customer is null) return Results.NotFound();
            customer.MarketingEmailConsent = request.Email;
            customer.MarketingSmsConsent = request.Sms;
            customer.MarketingWhatsAppConsent = request.WhatsApp;
            customer.ConsentUpdatedAtUtc = DateTimeOffset.UtcNow;
            Audit(db, tenant, customer.OrganizationId, "customer.consent_changed", customer.Id);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("customers.consent.manage");

        api.MapGet("/services/{id:guid}/resource-requirements", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ServiceResourceRequirements.Where(x => x.ServiceId == id).ToListAsync(ct)))
            .RequireAuthorization("resources.read");

        api.MapPut("/services/{id:guid}/resource-requirements", async (Guid id, ResourceRequirementRequest[] request, TenantContext tenant, AppDbContext db, CancellationToken ct) =>
        {
            if (tenant.TenantId is null) return Results.Unauthorized();
            var service = await db.SalonServices.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (service is null) return Results.NotFound();
            if (request.Any(x => x.QuantityRequired < 1)) return Invalid("quantityRequired", "Required quantity must be at least one.");
            var resourceIds = request.Where(x => x.SpecificResourceId.HasValue).Select(x => x.SpecificResourceId!.Value).Distinct().ToArray();
            if (await db.SalonResources.CountAsync(x => resourceIds.Contains(x.Id) && x.OrganizationId == service.OrganizationId && x.IsActive, ct) != resourceIds.Length)
                return Invalid("specificResourceId", "Specific resources must belong to the service organization.");
            db.ServiceResourceRequirements.RemoveRange(await db.ServiceResourceRequirements.Where(x => x.ServiceId == id).ToListAsync(ct));
            db.ServiceResourceRequirements.AddRange(request.Select(x => new ServiceResourceRequirement
            {
                TenantId = tenant.TenantId.Value,
                OrganizationId = service.OrganizationId,
                ServiceId = id,
                ResourceType = x.ResourceType,
                SpecificResourceId = x.SpecificResourceId,
                QuantityRequired = x.QuantityRequired,
                IsMandatory = x.IsMandatory
            }));
            service.RequiresResource = request.Any(x => x.IsMandatory);
            Audit(db, tenant, service.OrganizationId, "service.resource_requirements_changed", service.Id);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("resources.update");

        return endpoints;
    }

    private static IResult Invalid(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    private static void Audit(AppDbContext db, TenantContext tenant, Guid organizationId, string action, Guid entityId) =>
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenant.TenantId!.Value,
            OrganizationId = organizationId,
            ActorUserId = tenant.UserId,
            Action = action,
            EntityType = action.Split('.')[0],
            EntityId = entityId.ToString(),
            Source = "api",
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
}

public sealed record StaffConfigurationRequest(
    IReadOnlyList<StaffAssignmentInput> Assignments,
    IReadOnlyList<StaffCapabilityInput> Capabilities,
    IReadOnlyList<WorkingHoursInput> WorkingHours,
    IReadOnlyList<BreakRuleInput> Breaks);
public sealed record StaffAssignmentInput(Guid BranchId, DateOnly StartDate, DateOnly? EndDate, bool IsPrimary);
public sealed record StaffCapabilityInput(Guid ServiceId, Guid? BranchId, bool OnlineBookingEnabled, string SkillLevel = "Standard", decimal? PriceOverride = null, int? DurationOverrideMinutes = null);
public sealed record WorkingHoursInput(Guid BranchId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public sealed record BreakRuleInput(Guid BranchId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public sealed record AvailabilityOverrideRequest(Guid BranchId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string OverrideType, string? Reason);
public sealed record CustomerNoteRequest(string NoteType, string Content, bool IsSensitive);
public sealed record CustomerConsentRequest(bool Email, bool Sms, bool WhatsApp);
public sealed record ResourceRequirementRequest(string ResourceType, Guid? SpecificResourceId, int QuantityRequired, bool IsMandatory);
