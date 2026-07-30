using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;
namespace AtiqSalon.Api.Application;

public static class OperatingEndpoints
{
    public static IEndpointRouteBuilder MapOperatingApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapGet("/service-categories", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.ServiceCategories.OrderBy(x => x.DisplayOrder).ToListAsync(ct))).RequireAuthorization("services.read");
        api.MapPost("/service-categories", async (ServiceCategory item, TenantContext tenant, AppDbContext db, CancellationToken ct) => { item.TenantId = tenant.TenantId!.Value; db.ServiceCategories.Add(item); await Persist(db, tenant, item.OrganizationId, "service_category.created", item.Id, ct); return Results.Created($"/api/v1/service-categories/{item.Id}", item); }).RequireAuthorization("services.create");
        api.MapGet("/services", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.SalonServices.OrderBy(x => x.DisplayOrder).ToListAsync(ct))).RequireAuthorization("services.read");
        api.MapGet("/services/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) => await db.SalonServices.SingleOrDefaultAsync(x => x.Id == id, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireAuthorization("services.read");
        api.MapPost("/services", async (SalonService item, TenantContext tenant, AppDbContext db, CancellationToken ct) => { var errors = ServiceRules.Validate(item); if (errors.Count > 0) return Results.ValidationProblem(errors); if (!await db.ServiceCategories.AnyAsync(x => x.Id == item.CategoryId && x.OrganizationId == item.OrganizationId, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { { "categoryId", ["Category is outside the organization."] } }); item.TenantId = tenant.TenantId!.Value; item.Code = item.Code.Trim().ToUpperInvariant(); db.SalonServices.Add(item); await Persist(db, tenant, item.OrganizationId, "service.created", item.Id, ct); return Results.Created($"/api/v1/services/{item.Id}", item); }).RequireAuthorization("services.create");
        api.MapPost("/services/{id:guid}/deactivate", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) => { var item = await db.SalonServices.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound(); item.IsActive = false; await Persist(db, tenant, item.OrganizationId, "service.deactivated", item.Id, ct); return Results.NoContent(); }).RequireAuthorization("services.deactivate");
        api.MapGet("/staff", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.StaffMembers.OrderBy(x => x.DisplayName).ToListAsync(ct))).RequireAuthorization("staff.read");
        api.MapPost("/staff", async (StaffMember item, TenantContext tenant, AppDbContext db, CancellationToken ct) => { item.TenantId = tenant.TenantId!.Value; item.DisplayName = $"{item.FirstName} {item.LastName}".Trim(); db.StaffMembers.Add(item); await Persist(db, tenant, item.OrganizationId, "staff.created", item.Id, ct); return Results.Created($"/api/v1/staff/{item.Id}", item); }).RequireAuthorization("staff.create");
        api.MapPut("/staff/{id:guid}/service-capabilities", async (Guid id, StaffServiceCapability[] items, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (!await db.StaffMembers.AnyAsync(x => x.Id == id, ct)) return Results.NotFound(); foreach (var item in items) { item.Id = Guid.NewGuid(); item.TenantId = tenant.TenantId!.Value; item.StaffMemberId = id; } db.StaffServiceCapabilities.AddRange(items); await Persist(db, tenant, items.FirstOrDefault()?.OrganizationId, "staff.capabilities_changed", id, ct); return Results.NoContent(); }).RequireAuthorization("staff.capabilities.manage");
        api.MapPut("/staff/{id:guid}/working-hours", async (Guid id, StaffWorkingHours[] items, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (!await db.StaffMembers.AnyAsync(x => x.Id == id, ct)) return Results.NotFound(); foreach (var item in items) { item.Id = Guid.NewGuid(); item.TenantId = tenant.TenantId!.Value; item.StaffMemberId = id; } db.StaffWorkingHours.AddRange(items); await Persist(db, tenant, items.FirstOrDefault()?.OrganizationId, "staff.schedule_changed", id, ct); return Results.NoContent(); }).RequireAuthorization("staff.schedule.manage");
        api.MapGet("/customers", async (string? search, AppDbContext db, CancellationToken ct) => { var query = db.Customers.AsQueryable(); if (!string.IsNullOrWhiteSpace(search)) { var n = search.Trim().ToLowerInvariant(); query = query.Where(x => x.DisplayName.ToLower().Contains(n) || x.NormalizedEmail == n || x.NormalizedPhone == n); } return Results.Ok(await query.OrderBy(x => x.DisplayName).Take(100).ToListAsync(ct)); }).RequireAuthorization("customers.read");
        api.MapGet("/customers/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) => await db.Customers.SingleOrDefaultAsync(x => x.Id == id, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireAuthorization("customers.read");
        api.MapPost("/customers", async (Customer item, TenantContext tenant, AppDbContext db, CancellationToken ct) => { item.TenantId = tenant.TenantId!.Value; item.DisplayName = $"{item.FirstName} {item.LastName}".Trim(); item.NormalizedEmail = item.Email?.Trim().ToLowerInvariant(); item.NormalizedPhone = $"{item.PhoneCountryCode}{item.PhoneNumber}".Replace(" ", ""); var count = await db.Customers.CountAsync(x => x.OrganizationId == item.OrganizationId, ct); item.CustomerNumber = $"CUS-{count + 1:000000}"; db.Customers.Add(item); await Persist(db, tenant, item.OrganizationId, "customer.created", item.Id, ct); return Results.Created($"/api/v1/customers/{item.Id}", item); }).RequireAuthorization("customers.create");
        api.MapGet("/customers/{id:guid}/appointments", async (Guid id, AppDbContext db, CancellationToken ct) => Results.Ok(await db.Appointments.Where(x => x.CustomerId == id).OrderByDescending(x => x.StartAtUtc).ToListAsync(ct))).RequireAuthorization("customers.read");
        api.MapGet("/resources", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.SalonResources.OrderBy(x => x.Name).ToListAsync(ct))).RequireAuthorization("resources.read");
        api.MapPost("/resources", async (SalonResource item, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (item.Capacity < 1) return Results.ValidationProblem(new Dictionary<string, string[]> { { "capacity", ["Capacity must be at least one."] } }); item.TenantId = tenant.TenantId!.Value; db.SalonResources.Add(item); await Persist(db, tenant, item.OrganizationId, "resource.created", item.Id, ct); return Results.Created($"/api/v1/resources/{item.Id}", item); }).RequireAuthorization("resources.create");
        api.MapGet("/appointments", async (Guid? branchId, DateTimeOffset? from, DateTimeOffset? to, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (branchId.HasValue && !tenant.CanAccessBranch(branchId.Value)) return Results.Forbid(); var query = db.Appointments.AsQueryable(); if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.BranchId)); if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId); if (from.HasValue) query = query.Where(x => x.EndAtUtc > from); if (to.HasValue) query = query.Where(x => x.StartAtUtc < to); return Results.Ok(await query.OrderBy(x => x.StartAtUtc).Take(500).ToListAsync(ct)); }).RequireAuthorization("appointments.read");
        api.MapGet("/appointments/{id:guid}", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) => await db.Appointments.SingleOrDefaultAsync(x => x.Id == id, ct) is { } item && tenant.CanAccessBranch(item.BranchId) ? Results.Ok(new { appointment = item, services = await db.AppointmentServices.Where(x => x.AppointmentId == id).OrderBy(x => x.Sequence).ToListAsync(ct), resources = await db.AppointmentResourceReservations.Where(x => x.AppointmentId == id).ToListAsync(ct) }) : Results.NotFound()).RequireAuthorization("appointments.read");
        api.MapPost("/appointments", async (CreateAppointment request, BookingService booking, CancellationToken ct) => { var result = await booking.CreateAsync(request, ct); return result.IsSuccess ? Results.Created($"/api/v1/appointments/{result.Id}", result) : Results.Conflict(result); }).RequireAuthorization("appointments.create");
        Transition(api, "confirm", "Confirmed", "appointments.confirm"); Transition(api, "check-in", "CheckedIn", "appointments.checkin"); Transition(api, "start", "InProgress", "appointments.start"); Transition(api, "complete", "Completed", "appointments.complete"); Transition(api, "cancel", "Cancelled", "appointments.cancel"); Transition(api, "no-show", "NoShow", "appointments.mark_no_show");
        api.MapPost("/appointments/{id:guid}/reschedule", async (Guid id, RescheduleAppointment request, BookingService booking, CancellationToken ct) => { var result = await booking.RescheduleAsync(id, request, ct); return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result); }).RequireAuthorization("appointments.reschedule");
        api.MapPut("/appointments/{id:guid}", async (Guid id, EditAppointment request, BookingService booking, CancellationToken ct) => { var result = await booking.EditAsync(id, request, ct); return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result); }).RequireAuthorization("appointments.reschedule");
        api.MapGet("/appointments/{id:guid}/history", async (Guid id, TenantContext tenant, AppDbContext db, CancellationToken ct) => await db.Appointments.AnyAsync(x => x.Id == id && (tenant.HasOrganizationWideAccess || tenant.BranchIds.Contains(x.BranchId)), ct) ? Results.Ok(await db.AppointmentStatusHistories.Where(x => x.AppointmentId == id).OrderBy(x => x.ChangedAtUtc).ToListAsync(ct)) : Results.NotFound()).RequireAuthorization("appointments.read");
        api.MapGet("/availability", async (Guid branchId, Guid serviceId, DateOnly dateFrom, DateOnly dateTo, Guid? preferredStaffMemberId, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (!tenant.CanAccessBranch(branchId)) return Results.Forbid(); var branch = await db.Branches.SingleOrDefaultAsync(x => x.Id == branchId && x.IsActive, ct); var service = await db.SalonServices.SingleOrDefaultAsync(x => x.Id == serviceId && x.IsActive, ct); if (branch is null || service is null || tenant.TenantId is null) return Results.NotFound(); var staff = await db.StaffServiceCapabilities.Where(x => x.ServiceId == serviceId && x.CanPerform && (x.BranchId == null || x.BranchId == branchId) && (preferredStaffMemberId == null || x.StaffMemberId == preferredStaffMemberId)).Select(x => x.StaffMemberId).Distinct().ToListAsync(ct); return Results.Ok(await Slots(db, tenant.TenantId.Value, branch, service, staff, dateFrom, dateTo, ct)); }).RequireAuthorization("appointments.read");
        var publicApi = api.MapGroup("/public/booking").AllowAnonymous().RequireRateLimiting("public-booking");
        publicApi.MapGet("/{organizationSlug}", async (string organizationSlug, AppDbContext db, CancellationToken ct) =>
        {
            var organization = await db.Organizations.IgnoreQueryFilters().Where(x => x.Slug == organizationSlug && x.Status == "active")
                .Select(x => new { x.Id, x.TenantId, x.TradingName, x.Slug, x.DefaultLanguage, x.TimeZone }).SingleOrDefaultAsync(ct);
            if (organization is null) return Results.NotFound();
            var branches = await db.Branches.IgnoreQueryFilters().Where(x => x.TenantId == organization.TenantId && x.OrganizationId == organization.Id && x.IsActive)
                .OrderBy(x => x.Name).Select(x => new { x.Name, code = x.Code.ToLower(), x.City, x.TimeZone }).ToListAsync(ct);
            return Results.Ok(new { organization.TradingName, organization.Slug, organization.DefaultLanguage, organization.TimeZone, branches });
        });
        publicApi.MapGet("/{organizationSlug}/{branchCode}/services", async (string organizationSlug, string branchCode, AppDbContext db, CancellationToken ct) =>
        {
            var scope = await PublicScope(db, organizationSlug, branchCode, ct);
            if (scope is null) return Results.NotFound();
            var branchSettings = await db.BranchBookingSettings.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == scope.Organization.TenantId && x.BranchId == scope.Branch.Id, ct);
            if (branchSettings is { OnlineBookingEnabled: false }) return Results.NotFound();
            var enabled = db.BranchServices.IgnoreQueryFilters().Where(x => x.TenantId == scope.Organization.TenantId && x.BranchId == scope.Branch.Id && x.IsActive);
            return Results.Ok(await db.SalonServices.IgnoreQueryFilters()
                .Where(x => x.TenantId == scope.Organization.TenantId && x.OrganizationId == scope.Organization.Id && x.IsActive && x.OnlineBookingEnabled
                    && (!db.BranchServices.IgnoreQueryFilters().Any(b => b.TenantId == scope.Organization.TenantId && b.ServiceId == x.Id)
                        || enabled.Any(b => b.ServiceId == x.Id && b.OnlineBookingEnabledOverride != false)))
                .OrderBy(x => x.DisplayOrder).Select(x => new { x.Id, x.Name, x.DurationMinutes, x.CleanupMinutes, x.BasePrice, x.CurrencyCode, x.DepositType, x.DepositValue }).ToListAsync(ct));
        });
        publicApi.MapGet("/{organizationSlug}/{branchCode}/staff", async (string organizationSlug, string branchCode, Guid serviceId, AppDbContext db, CancellationToken ct) =>
        {
            var scope = await PublicScope(db, organizationSlug, branchCode, ct);
            if (scope is null) return Results.NotFound();
            var people = await db.StaffServiceCapabilities.IgnoreQueryFilters()
                .Where(x => x.TenantId == scope.Organization.TenantId && x.OrganizationId == scope.Organization.Id && x.ServiceId == serviceId
                    && x.CanPerform && x.OnlineBookingEnabled && (x.BranchId == null || x.BranchId == scope.Branch.Id))
                .Join(db.StaffMembers.IgnoreQueryFilters().Where(x => x.TenantId == scope.Organization.TenantId && x.IsActive
                    && x.EmploymentStatus == "Active" && x.OnlineBookingEnabled),
                    capability => capability.StaffMemberId, person => person.Id, (capability, person) => new { person.Id, person.DisplayName })
                .Distinct().OrderBy(x => x.DisplayName).ToListAsync(ct);
            return Results.Ok(people);
        });
        publicApi.MapGet("/{organizationSlug}/{branchCode}/availability", async (string organizationSlug, string branchCode, Guid serviceId, DateOnly dateFrom, DateOnly dateTo, Guid? preferredStaffMemberId, AppDbContext db, CancellationToken ct) =>
        {
            var scope = await PublicScope(db, organizationSlug, branchCode, ct);
            if (scope is null || dateTo < dateFrom || dateTo > dateFrom.AddDays(31)) return Results.NotFound();
            var service = await db.SalonServices.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == scope.Organization.TenantId
                && x.OrganizationId == scope.Organization.Id && x.Id == serviceId && x.IsActive && x.OnlineBookingEnabled, ct);
            if (service is null) return Results.NotFound();
            var staff = await db.StaffServiceCapabilities.IgnoreQueryFilters().Where(x => x.TenantId == scope.Organization.TenantId
                && x.ServiceId == serviceId && x.CanPerform && x.OnlineBookingEnabled && (x.BranchId == null || x.BranchId == scope.Branch.Id)
                && (preferredStaffMemberId == null || x.StaffMemberId == preferredStaffMemberId))
                .Join(db.StaffMembers.IgnoreQueryFilters().Where(x => x.TenantId == scope.Organization.TenantId && x.IsActive
                    && x.EmploymentStatus == "Active" && x.OnlineBookingEnabled), x => x.StaffMemberId, x => x.Id, (x, _) => x.StaffMemberId)
                .Distinct().ToListAsync(ct);
            return Results.Ok(await Slots(db, scope.Organization.TenantId, scope.Branch, service, staff, dateFrom, dateTo, ct));
        });
        publicApi.MapPost("/{organizationSlug}/{branchCode}/appointments", async (string organizationSlug, string branchCode, PublicBookingRequest request, AppDbContext db, BookingService booking, CancellationToken ct) =>
        {
            var scope = await PublicScope(db, organizationSlug, branchCode, ct);
            if (scope is null || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["booking"] = ["The booking request is invalid."] });
            var organizationSettings = await db.OrganizationBookingSettings.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == scope.Organization.TenantId && x.OrganizationId == scope.Organization.Id, ct);
            var branchSettings = await db.BranchBookingSettings.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == scope.Organization.TenantId && x.BranchId == scope.Branch.Id, ct);
            if (branchSettings is { OnlineBookingEnabled: false } || branchSettings?.AllowGuestBooking == false || (branchSettings?.AllowGuestBooking is null && organizationSettings?.AllowGuestBooking == false))
                return Results.NotFound();
            var email = request.Email?.Trim().ToLowerInvariant();
            var phone = $"{request.PhoneCountryCode}{request.PhoneNumber}".Replace(" ", "");
            if ((organizationSettings?.RequireEmail == true && string.IsNullOrWhiteSpace(email))
                || (organizationSettings?.RequirePhone != false && string.IsNullOrWhiteSpace(request.PhoneNumber)))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["contact"] = ["Required contact information is missing."] });
            var customer = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == scope.Organization.TenantId
                && x.OrganizationId == scope.Organization.Id && ((email != null && x.NormalizedEmail == email) || x.NormalizedPhone == phone), ct);
            if (customer is null)
            {
                customer = new Customer
                {
                    TenantId = scope.Organization.TenantId,
                    OrganizationId = scope.Organization.Id,
                    CustomerNumber = $"CUS-{Random.Shared.Next(1, 1000000):000000}",
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    DisplayName = $"{request.FirstName} {request.LastName}".Trim(),
                    Email = email,
                    NormalizedEmail = email,
                    PhoneCountryCode = request.PhoneCountryCode,
                    PhoneNumber = request.PhoneNumber,
                    NormalizedPhone = phone,
                    PreferredLanguage = request.Language ?? "en",
                    Source = "Online"
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);
            }
            if (customer.IsBlocked) return Results.Conflict(new { message = "This booking could not be completed." });
            var status = (branchSettings?.AutoConfirmOnlineBookings ?? organizationSettings?.AutoConfirmOnlineBookings ?? false) ? "Confirmed" : "PendingConfirmation";
            var result = await booking.CreateForTenantAsync(scope.Organization.TenantId, null,
                new CreateAppointment(scope.Organization.Id, scope.Branch.Id, customer.Id, request.StartAtUtc, request.EndAtUtc,
                    [new AppointmentServiceSelection(request.ServiceId, request.StaffMemberId)], "Online", status, request.CustomerNotes, request.IdempotencyKey), ct);
            return result.IsSuccess ? Results.Ok(new { result.Number, result.IsReplay, status }) : Results.Conflict(new { message = "This booking could not be completed.", result.Code });
        });
        return endpoints;
    }
    private static void Transition(RouteGroupBuilder api, string route, string status, string permission) => api.MapPost($"/appointments/{{id:guid}}/{route}", async (Guid id, TransitionRequest request, BookingService booking, CancellationToken ct) => await booking.TransitionAsync(id, status, request.Reason, ct) ? Results.NoContent() : Results.Conflict(new { message = "Invalid appointment transition." })).RequireAuthorization(permission);
    private static async Task Persist(AppDbContext db, TenantContext tenant, Guid? organizationId, string action, Guid entityId, CancellationToken ct) { db.AuditEvents.Add(new AuditEvent { TenantId = tenant.TenantId!.Value, OrganizationId = organizationId, ActorUserId = tenant.UserId, Action = action, EntityType = action.Split('.')[0], EntityId = entityId.ToString(), Source = "api", OccurredAtUtc = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); }
    private static async Task<PublicScopeResult?> PublicScope(AppDbContext db, string organizationSlug, string branchCode, CancellationToken ct)
    {
        var organization = await db.Organizations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Slug == organizationSlug && x.Status == "active", ct);
        if (organization is null) return null;
        var branch = await db.Branches.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == organization.TenantId
            && x.OrganizationId == organization.Id && x.Code.ToLower() == branchCode.ToLower() && x.IsActive, ct);
        return branch is null ? null : new(organization, branch);
    }
    private static async Task<IReadOnlyList<object>> Slots(AppDbContext db, Guid tenantId, Branch branch, SalonService service, IReadOnlyCollection<Guid> staff, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var output = new List<object>();
        var organizationSettings = await db.OrganizationBookingSettings.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.OrganizationId == branch.OrganizationId, ct);
        var branchSettings = await db.BranchBookingSettings.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.BranchId == branch.Id, ct);
        var serviceRule = await db.ServiceBookingRules.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ServiceId == service.Id, ct);
        var intervalMinutes = branchSettings?.SlotIntervalMinutes ?? organizationSettings?.DefaultSlotIntervalMinutes ?? 15;
        var minimumLead = serviceRule?.MinimumLeadTimeMinutes ?? branchSettings?.MinimumAdvanceBookingMinutes ?? organizationSettings?.MinimumAdvanceBookingMinutes ?? 0;
        var maximumDays = serviceRule?.MaximumAdvanceDays ?? organizationSettings?.MaximumAdvanceBookingDays ?? 90;
        var earliest = DateTimeOffset.UtcNow.AddMinutes(minimumLead);
        var latest = DateTimeOffset.UtcNow.AddDays(maximumDays);
        var duration = service.DurationMinutes + service.ProcessingMinutes + service.CleanupMinutes;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZone);
        for (var date = from; date <= to && output.Count < 500; date = date.AddDays(1))
        {
            var hours = await db.BranchBusinessHours.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.BranchId == branch.Id
                && x.DayOfWeek == date.DayOfWeek && !x.IsClosed && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
                .OrderBy(x => x.OpenTime).ToListAsync(ct);
            foreach (var opening in hours)
                foreach (var person in staff.Order())
                    for (var local = opening.OpenTime; local.AddMinutes(duration) <= opening.CloseTime; local = local.AddMinutes(intervalMinutes))
                    {
                        var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(local), zone), TimeSpan.Zero);
                        var end = start.AddMinutes(duration);
                        if (start < earliest || start > latest) continue;
                        if (await db.BranchClosures.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.BranchId == branch.Id && x.StartsAtUtc < end && start < x.EndsAtUtc, ct)) continue;
                        var working = await db.StaffWorkingHours.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.BranchId == branch.Id
                            && x.StaffMemberId == person && x.DayOfWeek == date.DayOfWeek && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).ToListAsync(ct);
                        if (working.Count > 0 && !working.Any(x => x.StartTime <= local && x.EndTime >= local.AddMinutes(duration))) continue;
                        if (await db.StaffBreakRules.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.BranchId == branch.Id
                            && x.StaffMemberId == person && x.DayOfWeek == date.DayOfWeek && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)
                            && x.StartTime < local.AddMinutes(duration) && local < x.EndTime, ct)) continue;
                        var overrides = await db.StaffAvailabilityOverrides.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.BranchId == branch.Id
                            && x.StaffMemberId == person && x.StartsAtUtc < end && start < x.EndsAtUtc).ToListAsync(ct);
                        if (!overrides.Any(x => x.OverrideType == "Available") && overrides.Any(x => x.OverrideType != "Available")) continue;
                        var conflict = await db.AppointmentServices.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.StaffMemberId == person
                            && x.StartAtUtc < end && start < x.EndAtUtc.AddMinutes(x.CleanupMinutes)
                            && db.Appointments.IgnoreQueryFilters().Any(a => a.Id == x.AppointmentId && a.Status != "Cancelled" && a.Status != "NoShow" && a.Status != "Completed"), ct);
                        if (!conflict) output.Add(new { date, startTime = start, endTime = end, eligibleStaff = new[] { person }, availableResources = Array.Empty<Guid>(), serviceSequence = new[] { service.Id } });
                    }
        }
        return output;
    }
}
public sealed record TransitionRequest(string? Reason);
public sealed record PublicBookingRequest(Guid ServiceId, Guid StaffMemberId, DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc, string FirstName, string LastName, string? Email, string PhoneCountryCode, string PhoneNumber, string IdempotencyKey, string? Language = "en", string? CustomerNotes = null);
public sealed record PublicScopeResult(Organization Organization, Branch Branch);
