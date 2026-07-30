using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Application;
using AtiqSalon.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
    var connectionString = builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("DATABASE_URL is required.");
    var signingKey = builder.Configuration["JWT_SIGNING_KEY"] ?? (builder.Environment.IsDevelopment() ? "development-signing-key-change-before-sharing" : throw new InvalidOperationException("JWT_SIGNING_KEY is required."));
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<BookingService>();
    builder.Services.AddScoped<CommercialService>();
    builder.Services.AddScoped<CommercialControlsService>();
    builder.Services.AddScoped<DepositService>();
    builder.Services.AddScoped<PackageService>();
    builder.Services.AddScoped<MembershipService>();
    builder.Services.AddScoped<GiftCardService>();
    builder.Services.AddScoped<CommissionService>();
    builder.Services.AddScoped<DailyClosingService>();
    builder.Services.AddScoped<CommercialCompletionService>();
    builder.Services.AddScoped<InventoryService>();
    builder.Services.AddScoped<PurchasingService>();
    builder.Services.AddScoped<ConsumptionService>();
    builder.Services.AddScoped<TransferService>();
    builder.Services.AddScoped<InventoryControlService>();
    builder.Services.AddScoped<WorkforceService>();
    builder.Services.AddScoped<WorkforceAdministrationService>();
    builder.Services.AddScoped<PerformanceAndLoyaltyService>();
    builder.Services.AddScoped<AiGovernanceService>();
    builder.Services.AddScoped<SaasAdministrationService>();
    builder.Services.AddScoped<MobileExperienceService>();
    builder.Services.AddHttpClient<IqaiPortalService>(client => client.Timeout = TimeSpan.FromSeconds(90));
    builder.Services.AddHostedService<NotificationDispatcher>();
    builder.Services.AddCors(options => options.AddPolicy("first-party-apps", policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("CORS_ORIGINS").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:3001"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
    builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("public-booking", limiter => { limiter.PermitLimit = 30; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; }));
    builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("mobile-auth", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(5); limiter.QueueLimit = 0; }));
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgres").AddRedis(builder.Configuration["REDIS_URL"] ?? "localhost:6379", name: "redis");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "atiqsalon-api", ValidateAudience = true, ValidAudience = "atiqsalon-portal", ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), ClockSkew = TimeSpan.FromSeconds(30) };
        options.Events = new JwtBearerEvents { OnMessageReceived = context => { context.Token = context.Request.Cookies["atiqsalon_customer_session"] ?? context.Request.Cookies["atiqsalon_staff_session"] ?? context.Request.Cookies["atiqsalon_session"]; return Task.CompletedTask; } };
    });
    builder.Services.AddAuthorization(options => PermissionCatalog.All.Concat(PermissionCatalog.InventoryAll).Concat(PermissionCatalog.WorkforceAll).Concat(PermissionCatalog.GrowthAll).Concat(PermissionCatalog.AiAll).Concat(PermissionCatalog.PlatformAll).ToList()
        .ForEach(permission => options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
            context.User.HasClaim("permission", permission)
            || PermissionCatalog.ForRoles(context.User.FindAll(ClaimTypes.Role).Select(x => x.Value)).Contains(permission)))));
    var app = builder.Build();
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("first-party-apps");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapOpenApi("/api/v1/openapi/{documentName}.json");
    app.MapHealthChecks("/api/v1/health", new HealthCheckOptions { ResponseWriter = async (context, report) => await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), checks = report.Entries.Select(x => new { name = x.Key, status = x.Value.Status.ToString() }) }) });

    var api = app.MapGroup("/api/v1");
    var auth = api.MapGroup("/auth");
    auth.MapPost("/register", async (RegisterRequest request, AppDbContext db, IPasswordHasher<User> hasher, CancellationToken ct) =>
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (request.Password.Length < 12 || await db.Users.IgnoreQueryFilters().AnyAsync(x => x.NormalizedEmail == email, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { { "credentials", ["Email is unavailable or the password is too short."] } });
        var tenant = new Tenant { Name = request.OrganizationName.Trim(), Slug = Slug.Create(request.OrganizationName) };
        var organization = new Organization { TenantId = tenant.Id, LegalName = request.OrganizationName.Trim(), TradingName = request.OrganizationName.Trim(), Slug = tenant.Slug, Email = email, CountryCode = request.CountryCode, DefaultCurrency = request.Currency, DefaultLanguage = request.Language, TimeZone = request.TimeZone };
        var user = new User { TenantId = tenant.Id, Email = email, NormalizedEmail = email, DisplayName = request.DisplayName.Trim(), EmailVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), Roles = ["OrganizationOwner"] };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.AddRange(tenant, organization, user, new AuditEvent { TenantId = tenant.Id, OrganizationId = organization.Id, ActorUserId = user.Id, Action = "tenant.created", EntityType = "Tenant", EntityId = tenant.Id.ToString(), Source = "api", OccurredAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new { message = "Registration accepted. Verify the email before signing in." });
    }).AllowAnonymous();
    auth.MapPost("/login", async (LoginRequest request, AppDbContext db, IPasswordHasher<User> hasher, HttpContext http, CancellationToken ct) =>
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        if (user is null || user.Status != "active" || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            if (user is not null) { user.FailedLoginCount++; db.AuditEvents.Add(AuditEvent.Security(user, "auth.login.failed", http)); await db.SaveChangesAsync(ct); }
            return Results.Problem(statusCode: 401, title: "Invalid credentials");
        }
        user.FailedLoginCount = 0; user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        var permissions = PermissionCatalog.ForRoles(user.Roles);
        var branchIds = await db.UserBranchAssignments.IgnoreQueryFilters().Where(x => x.TenantId == user.TenantId && x.UserId == user.Id && x.IsActive).Select(x => x.BranchId).ToListAsync(ct);
        var token = TokenFactory.Create(user, permissions, branchIds, signingKey);
        var refreshPlain = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
        db.RefreshSessions.Add(new RefreshSession { TenantId = user.TenantId, UserId = user.Id, TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshPlain))), ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(14) });
        db.AuditEvents.Add(AuditEvent.Security(user, "auth.login", http)); await db.SaveChangesAsync(ct);
        http.Response.Cookies.Append("atiqsalon_session", token, new CookieOptions { HttpOnly = true, Secure = !app.Environment.IsDevelopment(), SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddMinutes(15) });
        http.Response.Cookies.Append("atiqsalon_refresh", refreshPlain, new CookieOptions { HttpOnly = true, Secure = !app.Environment.IsDevelopment(), SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(14), Path = "/api/v1/auth" });
        return Results.Ok(new { user.Id, user.DisplayName, user.Email, permissions });
    }).AllowAnonymous();
    auth.MapPost("/logout", async (HttpContext http, AppDbContext db, CancellationToken ct) => { var userId = http.User.GetGuid("sub"); if (userId is not null) { var user = await db.Users.FindAsync([userId.Value], ct); if (user is not null) db.AuditEvents.Add(AuditEvent.Security(user, "auth.logout", http)); await db.SaveChangesAsync(ct); } http.Response.Cookies.Delete("atiqsalon_session"); http.Response.Cookies.Delete("atiqsalon_refresh", new CookieOptions { Path = "/api/v1/auth" }); return Results.NoContent(); }).RequireAuthorization();
    api.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) => { var id = principal.GetGuid("sub"); if (id is null) return Results.Unauthorized(); var user = await db.Users.Where(x => x.Id == id).SingleAsync(ct); return Results.Ok(new { user.Id, user.DisplayName, user.Email, user.TenantId, user.Roles, Permissions = PermissionCatalog.ForRoles(user.Roles) }); }).RequireAuthorization();
    api.MapGet("/organizations", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Organizations.Select(x => new { x.Id, x.LegalName, x.TradingName, x.CountryCode, x.DefaultCurrency, x.DefaultLanguage, x.TimeZone, x.Status }).ToListAsync(ct))).RequireAuthorization("organization.read");
    api.MapGet("/branches", async (TenantContext tenant, AppDbContext db, CancellationToken ct) => { var query = db.Branches.AsQueryable(); if (!tenant.HasOrganizationWideAccess) query = query.Where(x => tenant.BranchIds.Contains(x.Id)); return Results.Ok(await query.Select(x => new { x.Id, x.OrganizationId, x.Name, x.Code, x.City, x.CountryCode, x.TimeZone, x.IsActive }).ToListAsync(ct)); }).RequireAuthorization("branch.read");
    api.MapPost("/branches", async (CreateBranchRequest request, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (!tenant.TenantId.HasValue) return Results.Unauthorized(); var branch = new Branch { TenantId = tenant.TenantId.Value, OrganizationId = request.OrganizationId, Name = request.Name.Trim(), Code = request.Code.Trim().ToUpperInvariant(), CountryCode = request.CountryCode, TimeZone = request.TimeZone, City = request.City }; db.Branches.Add(branch); db.AuditEvents.Add(new AuditEvent { TenantId = branch.TenantId, OrganizationId = branch.OrganizationId, ActorUserId = tenant.UserId, Action = "branch.created", EntityType = "Branch", EntityId = branch.Id.ToString(), Source = "api", OccurredAtUtc = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/branches/{branch.Id}", new { branch.Id }); }).RequireAuthorization("branch.create");
    api.MapGet("/permissions", () => Results.Ok(PermissionCatalog.All)).RequireAuthorization("role.read");
    api.MapGet("/roles", () => Results.Ok(PermissionCatalog.Roles)).RequireAuthorization("role.read");
    api.MapPut("/users/{id:guid}/branch-assignments", async (Guid id, Guid[] branchIds, TenantContext tenant, AppDbContext db, CancellationToken ct) => { if (tenant.TenantId is null || !await db.Users.AnyAsync(x => x.Id == id, ct)) return Results.NotFound(); var branches = await db.Branches.Where(x => branchIds.Contains(x.Id) && x.IsActive).Select(x => new { x.Id, x.OrganizationId }).ToListAsync(ct); if (branches.Count != branchIds.Distinct().Count()) return Results.ValidationProblem(new Dictionary<string, string[]> { ["branchIds"] = ["Every branch must belong to this tenant."] }); var existing = await db.UserBranchAssignments.Where(x => x.UserId == id).ToListAsync(ct); db.UserBranchAssignments.RemoveRange(existing); db.UserBranchAssignments.AddRange(branches.Select(x => new UserBranchAssignment { TenantId = tenant.TenantId.Value, UserId = id, OrganizationId = x.OrganizationId, BranchId = x.Id })); db.AuditEvents.Add(new AuditEvent { TenantId = tenant.TenantId.Value, ActorUserId = tenant.UserId, Action = "user.branch_assignments_changed", EntityType = "User", EntityId = id.ToString(), Source = "api", OccurredAtUtc = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct); return Results.NoContent(); }).RequireAuthorization("role.assign");
    api.MapGet("/audit-events", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.AuditEvents.OrderByDescending(x => x.OccurredAtUtc).Take(100).Select(x => new { x.Id, x.Action, x.EntityType, x.EntityId, x.Source, x.OccurredAtUtc, x.CorrelationId }).ToListAsync(ct))).RequireAuthorization("audit.read");
    api.MapGet("/tenants", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Tenants.Select(x => new { x.Id, x.Name, x.Slug, x.Status }).ToListAsync(ct))).RequireAuthorization("platform.tenants.read");
    app.MapOperatingApi();
    app.MapManagementApi();
    app.MapCommercialApi();
    app.MapDepositApi();
    app.MapPackageApi();
    app.MapMembershipApi();
    app.MapGiftCardApi();
    app.MapCommissionApi();
    app.MapDailyClosingApi();
    app.MapCommercialReportApi();
    app.MapCommercialCompletionApi();
    app.MapInventoryApi();
    app.MapPurchasingApi();
    app.MapConsumptionApi();
    app.MapTransferApi();
    app.MapInventoryControlApi();
    app.MapInventoryReportApi();
    app.MapWorkforceApi();
    app.MapWorkforceAdministrationApi();
    app.MapPerformanceAndLoyaltyApi();
    app.MapIqaiPortalApi();
    app.MapAiGovernanceApi();
    app.MapSaasAdministrationApi();
    app.MapMobileExperienceApi();
    if (args.Contains("--seed-development", StringComparer.OrdinalIgnoreCase))
    {
        await DevelopmentSeeder.SeedAsync(app.Services, app.Environment);
        return;
    }
    app.Run();
}
catch (Exception exception) { Log.Fatal(exception, "API terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }

public partial class Program;
public sealed record RegisterRequest(string Email, string Password, string DisplayName, string OrganizationName, string CountryCode = "AE", string Currency = "AED", string Language = "en", string TimeZone = "Asia/Dubai");
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateBranchRequest(Guid OrganizationId, string Name, string Code, string CountryCode, string TimeZone, string? City);
