using System.Data;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class BookingService(AppDbContext db, TenantContext tenant, ConsumptionService consumption)
{
    private static readonly string[] BlockingStatuses = ["Draft", "PendingConfirmation", "Confirmed", "CheckedIn", "InProgress"];

    public Task<BookingResult> CreateAsync(CreateAppointment command, CancellationToken ct) =>
        tenant.TenantId is { } tenantId && tenant.CanAccessBranch(command.BranchId)
            ? CreateForTenantAsync(tenantId, tenant.UserId, command, ct)
            : Task.FromResult(BookingResult.Fail("unauthorized", "Access to the selected branch is required."));

    public async Task<BookingResult> CreateForTenantAsync(Guid tenantId, Guid? actorUserId, CreateAppointment command, CancellationToken ct)
    {
        if (command.Services.Count == 0 || command.StartAtUtc >= command.EndAtUtc)
            return BookingResult.Fail("validation", "A service and valid interval are required.");

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var replay = await db.Appointments.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.IdempotencyKey == command.IdempotencyKey, ct);
            if (replay is not null) return BookingResult.Success(replay.Id, replay.AppointmentNumber, true);
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({tenantId + ":" + command.BranchId}))", ct);

        var branch = await db.Branches.IgnoreQueryFilters().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == command.BranchId && x.OrganizationId == command.OrganizationId && x.IsActive, ct);
        var customer = await db.Customers.IgnoreQueryFilters().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == command.CustomerId && x.OrganizationId == command.OrganizationId && !x.IsBlocked, ct);
        if (branch is null || customer is null)
            return BookingResult.Fail("scope", "Branch or customer is unavailable.");

        var serviceIds = command.Services.Select(x => x.ServiceId).Distinct().ToArray();
        var staffIds = command.Services.Select(x => x.StaffMemberId).Distinct().ToArray();
        var services = await db.SalonServices.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.OrganizationId == command.OrganizationId && serviceIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);
        var staff = await db.StaffMembers.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.OrganizationId == command.OrganizationId && staffIds.Contains(x.Id) && x.IsActive && x.EmploymentStatus == "Active")
            .ToDictionaryAsync(x => x.Id, ct);
        if (services.Count != serviceIds.Length || staff.Count != staffIds.Length)
            return BookingResult.Fail("inactive", "A service or staff member is unavailable.");

        var zone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZone);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(command.StartAtUtc, zone).DateTime);
        var assignments = await db.StaffBranchAssignments.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.BranchId == branch.Id && staffIds.Contains(x.StaffMemberId) && x.IsActive
                && x.StartDate <= localDate && (x.EndDate == null || x.EndDate >= localDate))
            .Select(x => x.StaffMemberId).Distinct().ToListAsync(ct);
        if (staffIds.Any(x => !assignments.Contains(x) && staff[x].DefaultBranchId != branch.Id))
            return BookingResult.Fail("branch", "A selected staff member is not assigned to this branch.");

        var capabilities = await db.StaffServiceCapabilities.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.OrganizationId == command.OrganizationId && staffIds.Contains(x.StaffMemberId)
                && serviceIds.Contains(x.ServiceId) && x.CanPerform && (x.BranchId == null || x.BranchId == command.BranchId))
            .ToListAsync(ct);
        if (command.Services.Any(i => !capabilities.Any(x => x.StaffMemberId == i.StaffMemberId && x.ServiceId == i.ServiceId)))
            return BookingResult.Fail("capability", "Staff capability is required.");

        var branchServices = await db.BranchServices.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.BranchId == branch.Id && serviceIds.Contains(x.ServiceId) && x.IsActive)
            .ToDictionaryAsync(x => x.ServiceId, ct);
        var planned = new List<PlannedService>();
        var cursor = command.StartAtUtc;
        foreach (var selected in command.Services)
        {
            var service = services[selected.ServiceId];
            var capability = capabilities.Where(x => x.ServiceId == selected.ServiceId && x.StaffMemberId == selected.StaffMemberId)
                .OrderByDescending(x => x.BranchId.HasValue).First();
            branchServices.TryGetValue(service.Id, out var branchService);
            var duration = capability.DurationOverrideMinutes ?? branchService?.DurationOverrideMinutes ?? service.DurationMinutes;
            var price = capability.PriceOverride ?? branchService?.PriceOverride ?? service.BasePrice;
            var serviceEnd = cursor.AddMinutes(duration + service.ProcessingMinutes);
            var occupiedEnd = serviceEnd.AddMinutes(service.CleanupMinutes);
            planned.Add(new(selected, service, duration, price, cursor, serviceEnd, occupiedEnd));
            cursor = occupiedEnd;
        }
        if (cursor > command.EndAtUtc)
            return BookingResult.Fail("validation", "The supplied interval is shorter than the selected services.");

        foreach (var item in planned)
        {
            if (!await IsStaffAvailable(tenantId, command.BranchId, item.Selection.StaffMemberId, branch.TimeZone, item.StartsAtUtc, item.OccupiedUntilUtc, ct))
                return BookingResult.Fail("availability", "Staff or branch availability does not cover the selected interval.");
            var conflict = await db.AppointmentServices.IgnoreQueryFilters().AnyAsync(x =>
                x.TenantId == tenantId && x.StaffMemberId == item.Selection.StaffMemberId
                && x.StartAtUtc < item.OccupiedUntilUtc && item.StartsAtUtc < x.EndAtUtc.AddMinutes(x.CleanupMinutes)
                && db.Appointments.IgnoreQueryFilters().Any(a => a.Id == x.AppointmentId && BlockingStatuses.Contains(a.Status)), ct);
            if (conflict) return BookingResult.Fail("conflict", "The selected interval is no longer available.");
        }

        var resourcePlans = new Dictionary<int, List<(SalonResource Resource, int Quantity)>>();
        for (var index = 0; index < planned.Count; index++)
        {
            var item = planned[index];
            var requirements = await db.ServiceResourceRequirements.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.ServiceId == item.Service.Id && x.IsMandatory).ToListAsync(ct);
            foreach (var requirement in requirements)
            {
                var candidates = await db.SalonResources.IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.BranchId == branch.Id && x.IsActive
                        && (requirement.SpecificResourceId != null ? x.Id == requirement.SpecificResourceId : x.Type == requirement.ResourceType))
                    .OrderBy(x => x.Code).ToListAsync(ct);
                SalonResource? selected = null;
                foreach (var candidate in candidates)
                {
                    var reserved = await db.AppointmentResourceReservations.IgnoreQueryFilters()
                        .Where(x => x.TenantId == tenantId && x.ResourceId == candidate.Id
                            && x.StartsAtUtc < item.OccupiedUntilUtc && item.StartsAtUtc < x.EndsAtUtc)
                        .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
                    if (BookingRules.HasResourceCapacity(candidate.Capacity, reserved, requirement.QuantityRequired))
                    {
                        selected = candidate;
                        break;
                    }
                }
                if (selected is null) return BookingResult.Fail("resource_conflict", "A required resource is no longer available.");
                if (!resourcePlans.TryGetValue(index, out var allocations))
                    resourcePlans[index] = allocations = [];
                allocations.Add((selected, requirement.QuantityRequired));
            }
        }

        var sequence = await db.Appointments.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && x.OrganizationId == command.OrganizationId && x.CreatedAtUtc.Year == DateTimeOffset.UtcNow.Year, ct);
        var appointment = new Appointment
        {
            TenantId = tenantId,
            OrganizationId = command.OrganizationId,
            BranchId = command.BranchId,
            CustomerId = customer.Id,
            AppointmentNumber = $"APT-{DateTimeOffset.UtcNow.Year}-{sequence + 1:000000}",
            Status = command.Status,
            Source = command.Source,
            StartAtUtc = command.StartAtUtc,
            EndAtUtc = cursor,
            BranchTimeZone = branch.TimeZone,
            CustomerDisplayName = customer.DisplayName,
            CustomerPhone = $"{customer.PhoneCountryCode}{customer.PhoneNumber}",
            CustomerEmail = customer.Email,
            CustomerNotes = command.CustomerNotes,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            IdempotencyKey = command.IdempotencyKey,
            ConfirmedAtUtc = command.Status == "Confirmed" ? DateTimeOffset.UtcNow : null
        };
        db.Appointments.Add(appointment);
        for (var index = 0; index < planned.Count; index++)
        {
            var item = planned[index];
            var line = new AppointmentService
            {
                TenantId = tenantId,
                OrganizationId = command.OrganizationId,
                AppointmentId = appointment.Id,
                ServiceId = item.Service.Id,
                StaffMemberId = item.Selection.StaffMemberId,
                StartAtUtc = item.StartsAtUtc,
                EndAtUtc = item.EndsAtUtc,
                DurationMinutes = item.DurationMinutes,
                CleanupMinutes = item.Service.CleanupMinutes,
                UnitPrice = item.Price,
                DiscountAmount = 0,
                TaxAmount = 0,
                TotalAmount = item.Price,
                DepositType = item.Service.DepositType,
                DepositValue = item.Service.DepositValue,
                Sequence = index + 1
            };
            db.AppointmentServices.Add(line);
            if (resourcePlans.TryGetValue(index, out var allocations))
                foreach (var reservation in allocations)
                    db.AppointmentResourceReservations.Add(new AppointmentResourceReservation
                    {
                        TenantId = tenantId,
                        OrganizationId = command.OrganizationId,
                        AppointmentId = appointment.Id,
                        AppointmentServiceId = line.Id,
                        ResourceId = reservation.Resource.Id,
                        StartsAtUtc = item.StartsAtUtc,
                        EndsAtUtc = item.OccupiedUntilUtc,
                        Quantity = reservation.Quantity
                    });
        }
        db.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            TenantId = tenantId,
            OrganizationId = command.OrganizationId,
            AppointmentId = appointment.Id,
            PreviousStatus = "",
            NewStatus = appointment.Status,
            ChangedByUserId = actorUserId
        });
        db.AuditEvents.Add(Audit(appointment, "appointment.created", actorUserId));
        await ScheduleNotifications(appointment, ct);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return BookingResult.Success(appointment.Id, appointment.AppointmentNumber);
    }

    public async Task<bool> TransitionAsync(Guid id, string next, string? reason, CancellationToken ct)
    {
        await using var transaction = next == "Completed" && db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (appointment is null || !tenant.CanAccessBranch(appointment.BranchId) || !AppointmentLifecycle.CanTransition(appointment.Status, next)) return false;
        if (next == "Completed" && !(await consumption.ConsumeAppointment(id, ct)).IsSuccess) return false;
        var previous = appointment.Status;
        var now = DateTimeOffset.UtcNow;
        appointment.Status = next;
        appointment.UpdatedByUserId = tenant.UserId;
        if (next == "Confirmed") appointment.ConfirmedAtUtc = now;
        if (next == "CheckedIn") appointment.CheckedInAtUtc = now;
        if (next == "InProgress") appointment.StartedAtUtc = now;
        if (next == "Completed") appointment.CompletedAtUtc = now;
        if (next == "NoShow") appointment.NoShowMarkedAtUtc = now;
        if (next == "Cancelled")
        {
            appointment.CancelledAtUtc = now;
            appointment.CancelledByUserId = tenant.UserId;
            appointment.CancellationReason = reason;
            var pending = await db.NotificationMessages.Where(x => x.AppointmentId == id && (x.Status == "Pending" || x.Status == "Scheduled")).ToListAsync(ct);
            pending.ForEach(x => x.Status = "Cancelled");
        }
        db.AppointmentStatusHistories.Add(new AppointmentStatusHistory { TenantId = appointment.TenantId, OrganizationId = appointment.OrganizationId, AppointmentId = id, PreviousStatus = previous, NewStatus = next, ChangedByUserId = tenant.UserId, Reason = reason, ChangedAtUtc = now });
        db.AuditEvents.Add(Audit(appointment, $"appointment.{next.ToLowerInvariant()}", tenant.UserId));
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<BookingResult> RescheduleAsync(Guid id, RescheduleAppointment command, CancellationToken ct)
    {
        if (tenant.TenantId is null || command.StartAtUtc >= command.EndAtUtc)
            return BookingResult.Fail("validation", "A valid interval is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (appointment is null || !tenant.CanAccessBranch(appointment.BranchId) || appointment.Status is "Completed" or "Cancelled" or "NoShow")
            return BookingResult.Fail("unauthorized", "The appointment cannot be rescheduled.");
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({appointment.TenantId + ":" + appointment.BranchId}))", ct);
        var lines = await db.AppointmentServices.Where(x => x.AppointmentId == id).OrderBy(x => x.Sequence).ToListAsync(ct);
        var branch = await db.Branches.SingleAsync(x => x.Id == appointment.BranchId, ct);
        var cursor = command.StartAtUtc;
        foreach (var line in lines)
        {
            var occupiedEnd = cursor.AddMinutes(line.DurationMinutes + line.CleanupMinutes);
            if (!await IsStaffAvailable(appointment.TenantId, appointment.BranchId, line.StaffMemberId, branch.TimeZone, cursor, occupiedEnd, ct))
                return BookingResult.Fail("availability", "Staff or branch availability does not cover the selected interval.");
            var conflict = await db.AppointmentServices.AnyAsync(x => x.AppointmentId != id && x.StaffMemberId == line.StaffMemberId
                && x.StartAtUtc < occupiedEnd && cursor < x.EndAtUtc.AddMinutes(x.CleanupMinutes)
                && db.Appointments.Any(a => a.Id == x.AppointmentId && BlockingStatuses.Contains(a.Status)), ct);
            if (conflict) return BookingResult.Fail("conflict", "The selected interval is no longer available.");
            var serviceEnd = cursor.AddMinutes(line.DurationMinutes);
            var reservations = await db.AppointmentResourceReservations.Where(x => x.AppointmentServiceId == line.Id).ToListAsync(ct);
            foreach (var reservation in reservations)
            {
                var reserved = await db.AppointmentResourceReservations.Where(x => x.AppointmentId != id && x.ResourceId == reservation.ResourceId
                    && x.StartsAtUtc < occupiedEnd && cursor < x.EndsAtUtc).SumAsync(x => (int?)x.Quantity, ct) ?? 0;
                var capacity = await db.SalonResources.Where(x => x.Id == reservation.ResourceId).Select(x => x.Capacity).SingleAsync(ct);
                if (!BookingRules.HasResourceCapacity(capacity, reserved, reservation.Quantity))
                    return BookingResult.Fail("resource_conflict", "A required resource is no longer available.");
                reservation.StartsAtUtc = cursor;
                reservation.EndsAtUtc = occupiedEnd;
            }
            line.StartAtUtc = cursor;
            line.EndAtUtc = serviceEnd;
            cursor = occupiedEnd;
        }
        if (cursor > command.EndAtUtc) return BookingResult.Fail("validation", "The supplied interval is shorter than the appointment.");
        var previousStart = appointment.StartAtUtc;
        appointment.StartAtUtc = command.StartAtUtc;
        appointment.EndAtUtc = cursor;
        appointment.UpdatedByUserId = tenant.UserId;
        db.AppointmentStatusHistories.Add(new AppointmentStatusHistory { TenantId = appointment.TenantId, OrganizationId = appointment.OrganizationId, AppointmentId = appointment.Id, PreviousStatus = appointment.Status, NewStatus = appointment.Status, ChangedByUserId = tenant.UserId, Reason = $"Rescheduled from {previousStart:O}", ChangedAtUtc = DateTimeOffset.UtcNow });
        db.AuditEvents.Add(Audit(appointment, "appointment.rescheduled", tenant.UserId));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return BookingResult.Success(appointment.Id, appointment.AppointmentNumber);
    }

    public async Task<BookingResult> EditAsync(Guid id, EditAppointment command, CancellationToken ct)
    {
        if (tenant.TenantId is null || command.Services.Count == 0 || command.StartAtUtc >= command.EndAtUtc)
            return BookingResult.Fail("validation", "A service and valid interval are required.");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (appointment is null || !tenant.CanAccessBranch(appointment.BranchId)
            || appointment.Status is "Completed" or "Cancelled" or "NoShow")
            return BookingResult.Fail("unauthorized", "The appointment cannot be edited.");
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({appointment.TenantId + ":" + appointment.BranchId}))", ct);

        var branch = await db.Branches.SingleAsync(x => x.Id == appointment.BranchId, ct);
        var serviceIds = command.Services.Select(x => x.ServiceId).Distinct().ToArray();
        var staffIds = command.Services.Select(x => x.StaffMemberId).Distinct().ToArray();
        var services = await db.SalonServices
            .Where(x => serviceIds.Contains(x.Id) && x.OrganizationId == appointment.OrganizationId && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);
        var staff = await db.StaffMembers
            .Where(x => staffIds.Contains(x.Id) && x.OrganizationId == appointment.OrganizationId
                && x.IsActive && x.EmploymentStatus == "Active")
            .ToDictionaryAsync(x => x.Id, ct);
        if (services.Count != serviceIds.Length || staff.Count != staffIds.Length)
            return BookingResult.Fail("inactive", "A service or staff member is unavailable.");

        var zone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZone);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(command.StartAtUtc, zone).DateTime);
        var assignments = await db.StaffBranchAssignments
            .Where(x => x.BranchId == branch.Id && staffIds.Contains(x.StaffMemberId) && x.IsActive
                && x.StartDate <= localDate && (x.EndDate == null || x.EndDate >= localDate))
            .Select(x => x.StaffMemberId).Distinct().ToListAsync(ct);
        if (staffIds.Any(x => !assignments.Contains(x) && staff[x].DefaultBranchId != branch.Id))
            return BookingResult.Fail("branch", "A selected staff member is not assigned to this branch.");

        var capabilities = await db.StaffServiceCapabilities
            .Where(x => staffIds.Contains(x.StaffMemberId) && serviceIds.Contains(x.ServiceId) && x.CanPerform
                && (x.BranchId == null || x.BranchId == appointment.BranchId))
            .ToListAsync(ct);
        if (command.Services.Any(i => !capabilities.Any(x =>
                x.StaffMemberId == i.StaffMemberId && x.ServiceId == i.ServiceId)))
            return BookingResult.Fail("capability", "Staff capability is required.");

        var branchServices = await db.BranchServices
            .Where(x => x.BranchId == branch.Id && serviceIds.Contains(x.ServiceId) && x.IsActive)
            .ToDictionaryAsync(x => x.ServiceId, ct);
        var planned = new List<PlannedService>();
        var cursor = command.StartAtUtc;
        foreach (var selected in command.Services)
        {
            var service = services[selected.ServiceId];
            var capability = capabilities.Where(x =>
                    x.ServiceId == selected.ServiceId && x.StaffMemberId == selected.StaffMemberId)
                .OrderByDescending(x => x.BranchId.HasValue).First();
            branchServices.TryGetValue(service.Id, out var branchService);
            var duration = capability.DurationOverrideMinutes
                ?? branchService?.DurationOverrideMinutes ?? service.DurationMinutes;
            var price = capability.PriceOverride ?? branchService?.PriceOverride ?? service.BasePrice;
            var serviceEnd = cursor.AddMinutes(duration + service.ProcessingMinutes);
            var occupiedEnd = serviceEnd.AddMinutes(service.CleanupMinutes);
            planned.Add(new(selected, service, duration, price, cursor, serviceEnd, occupiedEnd));
            cursor = occupiedEnd;
        }
        if (cursor > command.EndAtUtc)
            return BookingResult.Fail("validation", "The supplied interval is shorter than the selected services.");

        foreach (var item in planned)
        {
            if (!await IsStaffAvailable(appointment.TenantId, appointment.BranchId,
                    item.Selection.StaffMemberId, branch.TimeZone, item.StartsAtUtc, item.OccupiedUntilUtc, ct))
                return BookingResult.Fail("availability", "Staff or branch availability does not cover the selected interval.");
            var conflict = await db.AppointmentServices.AnyAsync(x =>
                x.AppointmentId != id && x.StaffMemberId == item.Selection.StaffMemberId
                && x.StartAtUtc < item.OccupiedUntilUtc && item.StartsAtUtc < x.EndAtUtc.AddMinutes(x.CleanupMinutes)
                && db.Appointments.Any(a => a.Id == x.AppointmentId && BlockingStatuses.Contains(a.Status)), ct);
            if (conflict) return BookingResult.Fail("conflict", "The selected interval is no longer available.");
        }

        var resourcePlans = new Dictionary<int, List<(SalonResource Resource, int Quantity)>>();
        for (var index = 0; index < planned.Count; index++)
        {
            var item = planned[index];
            var requirements = await db.ServiceResourceRequirements
                .Where(x => x.ServiceId == item.Service.Id && x.IsMandatory).ToListAsync(ct);
            foreach (var requirement in requirements)
            {
                var candidates = await db.SalonResources
                    .Where(x => x.BranchId == branch.Id && x.IsActive
                        && (requirement.SpecificResourceId != null
                            ? x.Id == requirement.SpecificResourceId
                            : x.Type == requirement.ResourceType))
                    .OrderBy(x => x.Code).ToListAsync(ct);
                SalonResource? selected = null;
                foreach (var candidate in candidates)
                {
                    var reserved = await db.AppointmentResourceReservations
                        .Where(x => x.AppointmentId != id && x.ResourceId == candidate.Id
                            && x.StartsAtUtc < item.OccupiedUntilUtc && item.StartsAtUtc < x.EndsAtUtc)
                        .SumAsync(x => (int?)x.Quantity, ct) ?? 0;
                    if (BookingRules.HasResourceCapacity(candidate.Capacity, reserved, requirement.QuantityRequired))
                    {
                        selected = candidate;
                        break;
                    }
                }
                if (selected is null)
                    return BookingResult.Fail("resource_conflict", "A required resource is no longer available.");
                if (!resourcePlans.TryGetValue(index, out var allocations))
                    resourcePlans[index] = allocations = [];
                allocations.Add((selected, requirement.QuantityRequired));
            }
        }

        db.AppointmentResourceReservations.RemoveRange(
            await db.AppointmentResourceReservations.Where(x => x.AppointmentId == id).ToListAsync(ct));
        db.AppointmentServices.RemoveRange(
            await db.AppointmentServices.Where(x => x.AppointmentId == id).ToListAsync(ct));
        for (var index = 0; index < planned.Count; index++)
        {
            var item = planned[index];
            var line = new AppointmentService
            {
                TenantId = appointment.TenantId,
                OrganizationId = appointment.OrganizationId,
                AppointmentId = id,
                ServiceId = item.Service.Id,
                StaffMemberId = item.Selection.StaffMemberId,
                StartAtUtc = item.StartsAtUtc,
                EndAtUtc = item.EndsAtUtc,
                DurationMinutes = item.DurationMinutes,
                CleanupMinutes = item.Service.CleanupMinutes,
                UnitPrice = item.Price,
                TotalAmount = item.Price,
                DepositType = item.Service.DepositType,
                DepositValue = item.Service.DepositValue,
                Sequence = index + 1
            };
            db.AppointmentServices.Add(line);
            if (resourcePlans.TryGetValue(index, out var allocations))
                foreach (var reservation in allocations)
                    db.AppointmentResourceReservations.Add(new AppointmentResourceReservation
                    {
                        TenantId = appointment.TenantId,
                        OrganizationId = appointment.OrganizationId,
                        AppointmentId = id,
                        AppointmentServiceId = line.Id,
                        ResourceId = reservation.Resource.Id,
                        StartsAtUtc = item.StartsAtUtc,
                        EndsAtUtc = item.OccupiedUntilUtc,
                        Quantity = reservation.Quantity
                    });
        }

        var previousSummary = $"{appointment.StartAtUtc:O}";
        appointment.StartAtUtc = command.StartAtUtc;
        appointment.EndAtUtc = cursor;
        appointment.CustomerNotes = command.CustomerNotes;
        appointment.InternalNotes = command.InternalNotes;
        appointment.UpdatedByUserId = tenant.UserId;
        db.AppointmentStatusHistories.Add(new AppointmentStatusHistory
        {
            TenantId = appointment.TenantId,
            OrganizationId = appointment.OrganizationId,
            AppointmentId = id,
            PreviousStatus = appointment.Status,
            NewStatus = appointment.Status,
            ChangedByUserId = tenant.UserId,
            Reason = $"Edited appointment. Previous start: {previousSummary}",
            ChangedAtUtc = DateTimeOffset.UtcNow
        });
        db.AuditEvents.Add(Audit(appointment, "appointment.edited", tenant.UserId));
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return BookingResult.Success(appointment.Id, appointment.AppointmentNumber);
    }

    private async Task<bool> IsStaffAvailable(Guid tenantId, Guid branchId, Guid staffId, string timeZone, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var localStart = TimeZoneInfo.ConvertTime(start, zone);
        var localEnd = TimeZoneInfo.ConvertTime(end, zone);
        if (localStart.Date != localEnd.Date) return false;
        var date = DateOnly.FromDateTime(localStart.Date);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime = TimeOnly.FromDateTime(localEnd.DateTime);
        var branchOpen = await db.BranchBusinessHours.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.BranchId == branchId
            && x.DayOfWeek == date.DayOfWeek && !x.IsClosed && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)
            && x.OpenTime <= startTime && x.CloseTime >= endTime, ct);
        if (!branchOpen) return false;
        if (await db.BranchClosures.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.BranchId == branchId && x.StartsAtUtc < end && start < x.EndsAtUtc, ct)) return false;
        var workingRules = await db.StaffWorkingHours.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.BranchId == branchId
            && x.StaffMemberId == staffId && x.DayOfWeek == date.DayOfWeek && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)).ToListAsync(ct);
        if (workingRules.Count > 0 && !workingRules.Any(x => x.StartTime <= startTime && x.EndTime >= endTime)) return false;
        if (await db.StaffBreakRules.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.BranchId == branchId
            && x.StaffMemberId == staffId && x.DayOfWeek == date.DayOfWeek && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date)
            && x.StartTime < endTime && startTime < x.EndTime, ct)) return false;
        var overrides = await db.StaffAvailabilityOverrides.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.BranchId == branchId
            && x.StaffMemberId == staffId && x.StartsAtUtc < end && start < x.EndsAtUtc).ToListAsync(ct);
        return overrides.Any(x => x.OverrideType == "Available") || !overrides.Any(x => x.OverrideType != "Available");
    }

    private async Task ScheduleNotifications(Appointment appointment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appointment.CustomerEmail)) return;
        var rules = await db.AppointmentReminderRules.IgnoreQueryFilters().Where(x => x.TenantId == appointment.TenantId
            && x.OrganizationId == appointment.OrganizationId && x.IsActive && x.Channel == "Email"
            && (x.BranchId == null || x.BranchId == appointment.BranchId)).ToListAsync(ct);
        foreach (var rule in rules)
        {
            var due = appointment.StartAtUtc.AddMinutes(-rule.MinutesBeforeAppointment);
            if (due <= DateTimeOffset.UtcNow) continue;
            db.NotificationMessages.Add(new NotificationMessage
            {
                TenantId = appointment.TenantId,
                OrganizationId = appointment.OrganizationId,
                BranchId = appointment.BranchId,
                CustomerId = appointment.CustomerId,
                AppointmentId = appointment.Id,
                Channel = "Email",
                TemplateCode = rule.TemplateCode,
                Recipient = appointment.CustomerEmail,
                Subject = $"Appointment reminder {appointment.AppointmentNumber}",
                Body = $"Your appointment {appointment.AppointmentNumber} is scheduled for {appointment.StartAtUtc:O}.",
                Status = "Scheduled",
                ScheduledForUtc = due,
                IdempotencyKey = $"{appointment.Id:N}:{rule.Id:N}"
            });
        }
    }

    private static AuditEvent Audit(Appointment a, string action, Guid? actor) => new()
    {
        TenantId = a.TenantId,
        OrganizationId = a.OrganizationId,
        ActorUserId = actor,
        Action = action,
        EntityType = "Appointment",
        EntityId = a.Id.ToString(),
        Source = "api",
        OccurredAtUtc = DateTimeOffset.UtcNow
    };

    private sealed record PlannedService(AppointmentServiceSelection Selection, SalonService Service, int DurationMinutes, decimal Price, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, DateTimeOffset OccupiedUntilUtc);
}

public static class BookingRules
{
    public static bool HasResourceCapacity(int capacity, int reserved, int requested) =>
        capacity > 0 && requested > 0 && reserved >= 0 && reserved + requested <= capacity;
}

public sealed record AppointmentServiceSelection(Guid ServiceId, Guid StaffMemberId);
public sealed record RescheduleAppointment(DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc);
public sealed record EditAppointment(DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc,
    IReadOnlyList<AppointmentServiceSelection> Services, string? CustomerNotes = null, string? InternalNotes = null);
public sealed record CreateAppointment(Guid OrganizationId, Guid BranchId, Guid CustomerId, DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc, IReadOnlyList<AppointmentServiceSelection> Services, string Source = "Reception", string Status = "Confirmed", string? CustomerNotes = null, string? IdempotencyKey = null);
public sealed record BookingResult(bool IsSuccess, Guid? Id, string? Number, string? Code, string? Message, bool IsReplay = false)
{
    public static BookingResult Success(Guid id, string number, bool replay = false) => new(true, id, number, null, null, replay);
    public static BookingResult Fail(string code, string message) => new(false, null, null, code, message);
}
