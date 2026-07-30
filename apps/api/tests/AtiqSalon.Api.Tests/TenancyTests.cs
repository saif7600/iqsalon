using System.Security.Claims;
using AtiqSalon.Api.Application;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace AtiqSalon.Api.Tests;

public sealed class TenancyTests
{
    [Fact]
    public async Task Global_filter_denies_rows_owned_by_another_tenant()
    {
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var http = new DefaultHttpContext(); http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", tenantA.ToString())], "test"));
        var accessor = new HttpContextAccessor { HttpContext = http }; var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options, new TenantContext(accessor));
        db.Organizations.AddRange(new Organization { TenantId = tenantA, LegalName = "A", TradingName = "A", Slug = "a", Email = "a@example.test" }, new Organization { TenantId = tenantB, LegalName = "B", TradingName = "B", Slug = "b", Email = "b@example.test" });
        await db.SaveChangesAsync();
        var visible = await db.Organizations.ToListAsync();
        Assert.Single(visible); Assert.Equal(tenantA, visible[0].TenantId);
    }
    [Fact]
    public void Roles_expand_to_permissions_without_business_logic_role_checks() => Assert.Contains("branch.create", PermissionCatalog.ForRoles(["OrganizationOwner"]));
    [Fact]
    public void Organization_owner_has_organization_wide_branch_access()
    {
        var context = TenantContextFor([new Claim(ClaimTypes.Role, "OrganizationOwner")]);
        Assert.True(context.CanAccessBranch(Guid.NewGuid()));
    }
    [Fact]
    public void Receptionist_is_limited_to_branch_claims()
    {
        var assigned = Guid.NewGuid();
        var context = TenantContextFor([new Claim(ClaimTypes.Role, "Receptionist"), new Claim("branch_id", assigned.ToString())]);
        Assert.True(context.CanAccessBranch(assigned));
        Assert.False(context.CanAccessBranch(Guid.NewGuid()));
    }
    [Fact]
    public async Task Booking_creation_rejects_an_unassigned_branch_before_database_work()
    {
        var tenantId = Guid.NewGuid();
        var assigned = Guid.NewGuid();
        var context = TenantContextFor([new Claim("tenant_id", tenantId.ToString()), new Claim(ClaimTypes.Role, "Receptionist"), new Claim("branch_id", assigned.ToString())]);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options, context);
        var inventory = new InventoryService(db, context);
        var consumption = new ConsumptionService(db, context, inventory);
        var service = new BookingService(db, context, consumption);
        var result = await service.CreateAsync(new CreateAppointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2), [new(Guid.NewGuid(), Guid.NewGuid())]), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("unauthorized", result.Code);
    }

    [Fact]
    public async Task PostgreSql_transaction_enforces_tenant_query_filter()
    {
        var connectionString = Environment.GetEnvironmentVariable("ATIQSALON_TEST_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var context = TenantContextFor([new Claim("tenant_id", tenantA.ToString())]);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new AppDbContext(options, context);
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Organizations.AddRange(
            new Organization { TenantId = tenantA, LegalName = "Integration A", TradingName = "Integration A", Slug = $"integration-a-{tenantA:N}", Email = "a@integration.test" },
            new Organization { TenantId = tenantB, LegalName = "Integration B", TradingName = "Integration B", Slug = $"integration-b-{tenantB:N}", Email = "b@integration.test" });
        await db.SaveChangesAsync();

        var visible = await db.Organizations
            .Where(x => x.TenantId == tenantA || x.TenantId == tenantB)
            .Select(x => x.TenantId)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal(tenantA, visible[0]);
        await transaction.RollbackAsync();
    }

    private static TenantContext TenantContextFor(IEnumerable<Claim> claims)
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new TenantContext(new HttpContextAccessor { HttpContext = http });
    }
}
