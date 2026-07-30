namespace AtiqSalon.Api.Domain;

public sealed class MobileVerificationChallenge : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public string Channel { get; set; } = "Email";
    public string DestinationHash { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public int FailedAttemptCount { get; set; }
}

public sealed class MobileSession : TenantEntity
{
    public Guid OrganizationId { get; set; }
    public string ActorType { get; set; } = "";
    public Guid? UserId { get; set; }
    public Guid? StaffMemberId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Channel { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}

public static class MobileSessionRules
{
    public const int VerificationLifetimeMinutes = 10;
    public const int MaximumVerificationAttempts = 5;
    public const int SessionLifetimeHours = 12;

    public static bool CanVerify(
        MobileVerificationChallenge challenge,
        DateTimeOffset now
    ) =>
        challenge.ConsumedAtUtc is null
        && challenge.ExpiresAtUtc > now
        && challenge.FailedAttemptCount < MaximumVerificationAttempts;

    public static bool IsActive(MobileSession session, DateTimeOffset now) =>
        session.RevokedAtUtc is null && session.ExpiresAtUtc > now;
}
