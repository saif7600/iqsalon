using AtiqSalon.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace AtiqSalon.Api.Data;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment environment, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Development seed is disabled outside Development.");
        await using var scope = services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(); await db.Database.MigrateAsync(ct);
        var tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == tenantId, ct))
        {
            await EnsureDevelopmentAccess(db, hasher, tenantId, Guid.Parse("20000000-0000-0000-0000-000000000001"), Guid.Parse("30000000-0000-0000-0000-000000000001"), ct);
            return;
        }
        var organizationId = Guid.Parse("20000000-0000-0000-0000-000000000001"); var branchA = Guid.Parse("30000000-0000-0000-0000-000000000001"); var branchB = Guid.Parse("30000000-0000-0000-0000-000000000002"); var categoryId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Fictional Pearl Studio", Slug = "fictional-pearl-studio" });
        db.Organizations.Add(new Organization { Id = organizationId, TenantId = tenantId, LegalName = "Fictional Pearl Studio LLC", TradingName = "Fictional Pearl Studio", Slug = "fictional-pearl-studio", Email = "hello@example.test" });
        db.Branches.AddRange(new Branch { Id = branchA, TenantId = tenantId, OrganizationId = organizationId, Name = "Fictional Marina", Code = "MARINA", City = "Dubai" }, new Branch { Id = branchB, TenantId = tenantId, OrganizationId = organizationId, Name = "Fictional Garden", Code = "GARDEN", City = "Dubai" });
        db.ServiceCategories.Add(new ServiceCategory { Id = categoryId, TenantId = tenantId, OrganizationId = organizationId, Name = "Studio Services" });
        var servicesList = Enumerable.Range(1, 10).Select(i => new SalonService { Id = Guid.Parse($"50000000-0000-0000-0000-{i:000000000000}"), TenantId = tenantId, OrganizationId = organizationId, CategoryId = categoryId, Name = $"Fictional Service {i}", Code = $"SVC-{i:00}", DurationMinutes = 30 + (i % 3) * 15, CleanupMinutes = 10, BasePrice = 75 + i * 10, OnlineBookingEnabled = true, DisplayOrder = i }).ToArray(); db.SalonServices.AddRange(servicesList);
        var staff = Enumerable.Range(1, 5).Select(i => new StaffMember { Id = Guid.Parse($"60000000-0000-0000-0000-{i:000000000000}"), TenantId = tenantId, OrganizationId = organizationId, EmployeeCode = $"TEAM-{i:00}", FirstName = "Fictional", LastName = $"Professional {i}", DisplayName = $"Fictional Professional {i}", DefaultBranchId = i % 2 == 0 ? branchB : branchA, OnlineBookingEnabled = true, AcceptsWalkIns = true }).ToArray(); db.StaffMembers.AddRange(staff);
        foreach (var person in staff)
        {
            db.StaffBranchAssignments.Add(new StaffBranchAssignment { TenantId = tenantId, OrganizationId = organizationId, StaffMemberId = person.Id, BranchId = person.DefaultBranchId!.Value, StartDate = new DateOnly(2026, 1, 1), IsPrimary = true });
            foreach (var day in Enum.GetValues<DayOfWeek>().Where(x => x != DayOfWeek.Sunday))
                db.StaffWorkingHours.Add(new StaffWorkingHours { TenantId = tenantId, OrganizationId = organizationId, StaffMemberId = person.Id, BranchId = person.DefaultBranchId.Value, DayOfWeek = day, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(20, 0), EffectiveFrom = new DateOnly(2026, 1, 1) });
            foreach (var service in servicesList.Take(5))
                db.StaffServiceCapabilities.Add(new StaffServiceCapability { TenantId = tenantId, OrganizationId = organizationId, StaffMemberId = person.Id, ServiceId = service.Id, BranchId = person.DefaultBranchId, CanPerform = true, OnlineBookingEnabled = true });
        }
        db.SalonResources.AddRange(Enumerable.Range(1, 5).Select(i => new SalonResource { TenantId = tenantId, OrganizationId = organizationId, BranchId = i % 2 == 0 ? branchB : branchA, Name = $"Fictional Station {i}", Code = $"ST-{i:00}", Type = "Station", Capacity = 1, OnlineBookingVisible = true }));
        foreach (var branch in new[] { branchA, branchB }) foreach (var day in Enum.GetValues<DayOfWeek>().Where(x => x != DayOfWeek.Sunday)) db.BranchBusinessHours.Add(new BranchBusinessHours { TenantId = tenantId, OrganizationId = organizationId, BranchId = branch, DayOfWeek = day, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(20, 0), EffectiveFrom = new DateOnly(2026, 1, 1) });
        db.OrganizationBookingSettings.Add(new OrganizationBookingSettings { TenantId = tenantId, OrganizationId = organizationId, DefaultSlotIntervalMinutes = 15, MinimumAdvanceBookingMinutes = 60, MaximumAdvanceBookingDays = 90, AllowGuestBooking = true, RequirePhone = true, AutoConfirmOnlineBookings = true });
        db.BranchBookingSettings.AddRange(new BranchBookingSettings { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchA, OnlineBookingEnabled = true }, new BranchBookingSettings { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchB, OnlineBookingEnabled = true });
        db.AppointmentReminderRules.Add(new AppointmentReminderRule { TenantId = tenantId, OrganizationId = organizationId, Channel = "Email", MinutesBeforeAppointment = 1440, TemplateCode = "appointment-reminder-24h" });
        await EnsureDevelopmentAccess(db, hasher, tenantId, organizationId, branchA, ct, false);
        var customers = Enumerable.Range(1, 20).Select(i => new Customer { Id = Guid.Parse($"70000000-0000-0000-0000-{i:000000000000}"), TenantId = tenantId, OrganizationId = organizationId, CustomerNumber = $"CUS-{i:000000}", FirstName = "Fictional", LastName = $"Guest {i}", DisplayName = $"Fictional Guest {i}", Email = $"guest{i}@example.test", NormalizedEmail = $"guest{i}@example.test", PhoneCountryCode = "+971", PhoneNumber = $"50000{i:00000}", NormalizedPhone = $"+97150000{i:00000}" }).ToArray(); db.Customers.AddRange(customers);
        for (var i = 0; i < 20; i++) { var start = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(i / 5 + 1).AddHours(6 + (i % 5) * 2); var appointment = new Appointment { TenantId = tenantId, OrganizationId = organizationId, BranchId = branchA, AppointmentNumber = $"APT-2026-{i + 1:000000}", CustomerId = customers[i].Id, CustomerDisplayName = customers[i].DisplayName, CustomerEmail = customers[i].Email, Status = i % 4 == 0 ? "PendingConfirmation" : "Confirmed", Source = "Reception", StartAtUtc = start, EndAtUtc = start.AddMinutes(servicesList[0].DurationMinutes), BranchTimeZone = "Asia/Dubai" }; db.Appointments.Add(appointment); db.AppointmentServices.Add(new AppointmentService { TenantId = tenantId, OrganizationId = organizationId, AppointmentId = appointment.Id, ServiceId = servicesList[0].Id, StaffMemberId = staff[i % staff.Length].Id, StartAtUtc = start, EndAtUtc = appointment.EndAtUtc, DurationMinutes = servicesList[0].DurationMinutes, CleanupMinutes = 10, UnitPrice = servicesList[0].BasePrice, TotalAmount = servicesList[0].BasePrice, Sequence = 1 }); }
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDevelopmentAccess(AppDbContext db, IPasswordHasher<User> hasher, Guid tenantId, Guid organizationId, Guid branchId, CancellationToken ct, bool save = true)
    {
        var ownerId = Guid.Parse("80000000-0000-0000-0000-000000000001");
        var receptionistId = Guid.Parse("80000000-0000-0000-0000-000000000002");
        var owner = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == ownerId, ct);
        if (owner is null)
        {
            owner = new User { Id = ownerId, TenantId = tenantId, Email = "owner@fictional-pearl.example.test", NormalizedEmail = "owner@fictional-pearl.example.test", DisplayName = "Fictional Owner", EmailVerified = true, Roles = ["OrganizationOwner"] };
            owner.PasswordHash = hasher.HashPassword(owner, "LocalDevelopment!2026");
            db.Users.Add(owner);
        }
        var receptionist = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == receptionistId, ct);
        if (receptionist is null)
        {
            receptionist = new User { Id = receptionistId, TenantId = tenantId, Email = "reception@fictional-pearl.example.test", NormalizedEmail = "reception@fictional-pearl.example.test", DisplayName = "Fictional Reception", EmailVerified = true, Roles = ["Receptionist"] };
            receptionist.PasswordHash = hasher.HashPassword(receptionist, "LocalDevelopment!2026");
            db.Users.Add(receptionist);
        }
        if (!await db.UserBranchAssignments.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.UserId == receptionistId && x.BranchId == branchId, ct))
            db.UserBranchAssignments.Add(new UserBranchAssignment { TenantId = tenantId, UserId = receptionistId, OrganizationId = organizationId, BranchId = branchId });
        if (save) await db.SaveChangesAsync(ct);
    }
}
