using AtiqSalon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public static class MobileExperienceEndpoints
{
    public static void MapMobileExperienceApi(this WebApplication app)
    {
        var customerAuth = app.MapGroup("/api/v1/customer-auth");
        customerAuth
            .MapPost(
                "/request-code",
                async (
                    RequestCustomerCode request,
                    MobileExperienceService service,
                    CancellationToken cancellationToken
                ) =>
                {
                    var requestId = Guid.NewGuid();
                    await service.RequestCustomerCodeAsync(
                        request,
                        requestId,
                        cancellationToken
                    );
                    return Results.Accepted(
                        value: new
                        {
                            message = "If the details are eligible, a verification code will be sent.",
                            requestId,
                        }
                    );
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting("mobile-auth");
        customerAuth
            .MapPost(
                "/verify",
                async (
                    VerifyCustomerCode request,
                    MobileExperienceService service,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await service.VerifyCustomerCodeAsync(
                        request,
                        cancellationToken
                    );
                    if (result is null)
                        return Results.Problem(statusCode: 401, title: "Verification failed");
                    SetCookie(
                        http,
                        "atiqsalon_customer_session",
                        result.Token,
                        result.ExpiresAtUtc
                    );
                    return Results.Ok(new { expiresAtUtc = result.ExpiresAtUtc });
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting("mobile-auth");
        customerAuth
            .MapPost(
                "/logout",
                async (
                    MobileExperienceService service,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var token = http.Request.Cookies["atiqsalon_customer_session"] ?? "";
                    var session = await service.ValidateSessionAsync(
                        http.User,
                        "Customer",
                        token,
                        cancellationToken
                    );
                    if (session is not null)
                        await service.RevokeAsync(
                            session,
                            "Customer signed out",
                            cancellationToken
                        );
                    DeleteCookie(http, "atiqsalon_customer_session");
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        var staffAuth = app.MapGroup("/api/v1/staff-auth");
        staffAuth
            .MapPost(
                "/login",
                async (
                    StaffMobileSignIn request,
                    MobileExperienceService service,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await service.SignInStaffAsync(request, cancellationToken);
                    if (result is null)
                        return Results.Problem(statusCode: 401, title: "Invalid credentials");
                    SetCookie(http, "atiqsalon_staff_session", result.Token, result.ExpiresAtUtc);
                    return Results.Ok(new { expiresAtUtc = result.ExpiresAtUtc });
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting("mobile-auth");
        staffAuth
            .MapPost(
                "/logout",
                async (
                    MobileExperienceService service,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var token = http.Request.Cookies["atiqsalon_staff_session"] ?? "";
                    var session = await service.ValidateSessionAsync(
                        http.User,
                        "Staff",
                        token,
                        cancellationToken
                    );
                    if (session is not null)
                        await service.RevokeAsync(session, "Staff signed out", cancellationToken);
                    DeleteCookie(http, "atiqsalon_staff_session");
                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        app.MapGet(
                "/api/v1/customer/me",
                async (
                    MobileExperienceService service,
                    AppDbContext db,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var token = http.Request.Cookies["atiqsalon_customer_session"] ?? "";
                    var session = await service.ValidateSessionAsync(
                        http.User,
                        "Customer",
                        token,
                        cancellationToken
                    );
                    if (session?.CustomerId is null)
                        return Results.Unauthorized();
                    var customer = await db
                        .Customers.IgnoreQueryFilters()
                        .Where(
                            x => x.TenantId == session.TenantId && x.Id == session.CustomerId
                        )
                        .Select(
                            x =>
                                new
                                {
                                    x.Id,
                                    x.DisplayName,
                                    x.Email,
                                    x.PreferredLanguage,
                                    x.PreferredBranchId,
                                    x.PreferredStaffMemberId,
                                }
                        )
                        .SingleAsync(cancellationToken);
                    return Results.Ok(customer);
                }
            )
            .RequireAuthorization();

        app.MapGet(
                "/api/v1/staff-app/me",
                async (
                    MobileExperienceService service,
                    AppDbContext db,
                    HttpContext http,
                    CancellationToken cancellationToken
                ) =>
                {
                    var token = http.Request.Cookies["atiqsalon_staff_session"] ?? "";
                    var session = await service.ValidateSessionAsync(
                        http.User,
                        "Staff",
                        token,
                        cancellationToken
                    );
                    if (session?.StaffMemberId is null)
                        return Results.Unauthorized();
                    var staff = await db
                        .StaffMembers.IgnoreQueryFilters()
                        .Where(
                            x =>
                                x.TenantId == session.TenantId
                                && x.Id == session.StaffMemberId
                        )
                        .Select(
                            x =>
                                new
                                {
                                    x.Id,
                                    x.DisplayName,
                                    x.JobTitle,
                                    x.PreferredLanguage,
                                    x.DefaultBranchId,
                                }
                        )
                        .SingleAsync(cancellationToken);
                    return Results.Ok(staff);
                }
            )
            .RequireAuthorization();
    }

    private static void SetCookie(
        HttpContext http,
        string name,
        string token,
        DateTimeOffset expiresAtUtc
    ) =>
        http.Response.Cookies.Append(
            name,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !http.Request.Host.Host.Contains("localhost"),
                SameSite = SameSiteMode.Lax,
                Expires = expiresAtUtc,
                Path = "/",
            }
        );

    private static void DeleteCookie(HttpContext http, string name) =>
        http.Response.Cookies.Delete(
            name,
            new CookieOptions
            {
                Secure = !http.Request.Host.Host.Contains("localhost"),
                SameSite = SameSiteMode.Lax,
                Path = "/",
            }
        );
}
