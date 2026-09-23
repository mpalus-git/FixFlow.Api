using ErrorOr;

namespace FixFlow.Api.Domain.Auth;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) =>
        Create(userId, Guid.CreateVersion7(), tokenHash, now, lifetime);

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public ErrorOr<RefreshToken> Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (IsRevoked)
        {
            return ReplacedByTokenId is null ? RefreshTokenErrors.Revoked : RefreshTokenErrors.Reused;
        }

        if (now >= ExpiresAt)
        {
            return RefreshTokenErrors.Expired;
        }

        var replacement = Create(UserId, FamilyId, newTokenHash, now, lifetime);
        RevokedAt = now;
        ReplacedByTokenId = replacement.Id;

        return replacement;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }

    private static RefreshToken Create(Guid userId, Guid familyId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        FamilyId = familyId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now.Add(lifetime),
    };
}
