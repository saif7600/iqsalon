using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AtiqSalon.Api.Application;

public sealed class MobileExperienceService(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration
)
{
    public async Task RequestCustomerCodeAsync(
        RequestCustomerCode request,
        Guid requestId,
        CancellationToken cancellationToken
    )
    {
        var identifier = request.Identifier.Trim().ToLowerInvariant();
        var organization = await db
            .Organizations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                x => x.Slug == request.OrganizationSlug.Trim().ToLowerInvariant(),
                cancellationToken
            );
        if (organization is null)
            return;

        var customer = await db
            .Customers.IgnoreQueryFilters()
            .Where(x => x.TenantId == organization.TenantId && x.OrganizationId == organization.Id)
            .SingleOrDefaultAsync(
                x => x.NormalizedEmail == identifier || x.NormalizedPhone == identifier,
                cancellationToken
            );
        if (customer is null || customer.IsBlocked)
            return;

        var recentCount = await db
            .MobileVerificationChallenges.IgnoreQueryFilters()
            .CountAsync(
                x =>
                    x.TenantId == customer.TenantId
                    && x.CustomerId == customer.Id
                    && x.CreatedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-15),
                cancellationToken
            );
        if (recentCount >= 3)
            return;

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new MobileVerificationChallenge
        {
            Id = requestId,
            TenantId = customer.TenantId,
            OrganizationId = customer.OrganizationId,
            CustomerId = customer.Id,
            Channel = customer.NormalizedEmail == identifier ? "Email" : "Sms",
            DestinationHash = Hash(identifier),
            CodeHash = Hash(code),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(
                MobileSessionRules.VerificationLifetimeMinutes
            ),
        };
        db.MobileVerificationChallenges.Add(challenge);
        db.NotificationMessages.Add(
            new NotificationMessage
            {
                TenantId = customer.TenantId,
                OrganizationId = customer.OrganizationId,
                CustomerId = customer.Id,
                Channel = challenge.Channel,
                TemplateCode = "customer.mobile.verification",
                Recipient = identifier,
                Body = $"Your AtiqSalon verification code is {code}. It expires in 10 minutes.",
                IdempotencyKey = $"mobile-verification:{challenge.Id:N}",
            }
        );
        db.AuditEvents.Add(
            MobileAudit(
                customer.TenantId,
                customer.OrganizationId,
                "customer.verification.requested",
                "MobileVerificationChallenge",
                challenge.Id
            )
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MobileSignInResult?> VerifyCustomerCodeAsync(
        VerifyCustomerCode request,
        CancellationToken cancellationToken
    )
    {
        var challenge = await db
            .MobileVerificationChallenges.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == request.ChallengeId, cancellationToken);
        if (
            challenge is null
            || !MobileSessionRules.CanVerify(challenge, DateTimeOffset.UtcNow)
        )
            return null;

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(challenge.CodeHash),
                Encoding.UTF8.GetBytes(Hash(request.Code.Trim()))
            ))
        {
            challenge.FailedAttemptCount++;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        challenge.ConsumedAtUtc = DateTimeOffset.UtcNow;
        var customer = await db
            .Customers.IgnoreQueryFilters()
            .SingleAsync(
                x => x.TenantId == challenge.TenantId && x.Id == challenge.CustomerId,
                cancellationToken
            );
        var result = CreateSession(
            customer.TenantId,
            customer.OrganizationId,
            "Customer",
            "CustomerApp",
            null,
            null,
            customer.Id,
            []
        );
        db.MobileSessions.Add(result.Session);
        db.AuditEvents.Add(
            MobileAudit(
                customer.TenantId,
                customer.OrganizationId,
                "customer.account.verified",
                "Customer",
                customer.Id
            )
        );
        await db.SaveChangesAsync(cancellationToken);
        return new MobileSignInResult(result.Token, result.Session.ExpiresAtUtc);
    }

    public async Task<MobileSignInResult?> SignInStaffAsync(
        StaffMobileSignIn request,
        CancellationToken cancellationToken
    )
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db
            .Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == email, cancellationToken);
        if (
            user is null
            || user.Status != "active"
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed
        )
            return null;

        var staff = await db
            .StaffMembers.IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                x => x.TenantId == user.TenantId && x.LinkedUserId == user.Id && x.IsActive,
                cancellationToken
            );
        if (staff is null)
            return null;

        var branchIds = await db
            .StaffBranchAssignments.IgnoreQueryFilters()
            .Where(x => x.TenantId == user.TenantId && x.StaffMemberId == staff.Id && x.IsActive)
            .Select(x => x.BranchId)
            .ToListAsync(cancellationToken);
        var result = CreateSession(
            user.TenantId,
            staff.OrganizationId,
            "Staff",
            "StaffApp",
            user.Id,
            staff.Id,
            null,
            branchIds
        );
        db.MobileSessions.Add(result.Session);
        db.AuditEvents.Add(
            MobileAudit(
                user.TenantId,
                staff.OrganizationId,
                "staff.mobile.signed_in",
                "StaffMember",
                staff.Id,
                user.Id
            )
        );
        await db.SaveChangesAsync(cancellationToken);
        return new MobileSignInResult(result.Token, result.Session.ExpiresAtUtc);
    }

    public async Task<MobileSession?> ValidateSessionAsync(
        ClaimsPrincipal principal,
        string expectedActorType,
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var sessionId = principal.GetGuid("mobile_session_id");
        if (sessionId is null || string.IsNullOrWhiteSpace(rawToken))
            return null;
        var session = await db
            .MobileSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId.Value, cancellationToken);
        if (
            session is null
            || session.ActorType != expectedActorType
            || !MobileSessionRules.IsActive(session, DateTimeOffset.UtcNow)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.TokenHash),
                Encoding.UTF8.GetBytes(Hash(rawToken))
            )
        )
            return null;
        session.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(
        MobileSession session,
        string reason,
        CancellationToken cancellationToken
    )
    {
        if (session.RevokedAtUtc is not null)
            return;
        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        session.RevocationReason = reason;
        db.AuditEvents.Add(
            MobileAudit(
                session.TenantId,
                session.OrganizationId,
                "mobile.session.revoked",
                "MobileSession",
                session.Id,
                session.UserId
            )
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private (string Token, MobileSession Session) CreateSession(
        Guid tenantId,
        Guid organizationId,
        string actorType,
        string channel,
        Guid? userId,
        Guid? staffMemberId,
        Guid? customerId,
        IEnumerable<Guid> branchIds
    )
    {
        var signingKey =
            configuration["JWT_SIGNING_KEY"]
            ?? throw new InvalidOperationException("JWT_SIGNING_KEY is required.");
        var session = new MobileSession
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            ActorType = actorType,
            Channel = channel,
            UserId = userId,
            StaffMemberId = staffMemberId,
            CustomerId = customerId,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(MobileSessionRules.SessionLifetimeHours),
        };
        var claims = new List<Claim>
        {
            new("sub", (userId ?? customerId ?? staffMemberId)!.Value.ToString()),
            new("tenant_id", tenantId.ToString()),
            new("organization_id", organizationId.ToString()),
            new("actor_type", actorType),
            new("mobile_session_id", session.Id.ToString()),
        };
        if (staffMemberId.HasValue)
            claims.Add(new Claim("staff_member_id", staffMemberId.Value.ToString()));
        if (customerId.HasValue)
            claims.Add(new Claim("customer_id", customerId.Value.ToString()));
        claims.AddRange(branchIds.Select(id => new Claim("branch_id", id.ToString())));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256
        );
        var token = new JwtSecurityToken(
            "atiqsalon-api",
            "atiqsalon-portal",
            claims,
            expires: session.ExpiresAtUtc.UtcDateTime,
            signingCredentials: credentials
        );
        var rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        session.TokenHash = Hash(rawToken);
        return (rawToken, session);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static AuditEvent MobileAudit(
        Guid tenantId,
        Guid organizationId,
        string action,
        string entityType,
        Guid entityId,
        Guid? actorUserId = null
    ) =>
        new()
        {
            TenantId = tenantId,
            OrganizationId = organizationId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Source = "mobile-api",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
}

public sealed record RequestCustomerCode(string OrganizationSlug, string Identifier);
public sealed record VerifyCustomerCode(Guid ChallengeId, string Code);
public sealed record StaffMobileSignIn(string Email, string Password);
public sealed record MobileSignInResult(string Token, DateTimeOffset ExpiresAtUtc);
