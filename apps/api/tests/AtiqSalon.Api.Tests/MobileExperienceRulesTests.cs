using AtiqSalon.Api.Domain;

namespace AtiqSalon.Api.Tests;

public sealed class MobileExperienceRulesTests
{
    [Fact]
    public void Verification_challenge_is_single_use()
    {
        var challenge = new MobileVerificationChallenge
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            ConsumedAtUtc = DateTimeOffset.UtcNow,
        };

        Assert.False(MobileSessionRules.CanVerify(challenge, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Verification_challenge_rejects_attempt_exhaustion()
    {
        var challenge = new MobileVerificationChallenge
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            FailedAttemptCount = MobileSessionRules.MaximumVerificationAttempts,
        };

        Assert.False(MobileSessionRules.CanVerify(challenge, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Revoked_mobile_session_is_inactive()
    {
        var session = new MobileSession
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            RevokedAtUtc = DateTimeOffset.UtcNow,
        };

        Assert.False(MobileSessionRules.IsActive(session, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expired_mobile_session_is_inactive()
    {
        var session = new MobileSession
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        };

        Assert.False(MobileSessionRules.IsActive(session, DateTimeOffset.UtcNow));
    }
}
