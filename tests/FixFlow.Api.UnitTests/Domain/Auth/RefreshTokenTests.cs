using FixFlow.Api.Domain.Auth;

namespace FixFlow.Api.UnitTests.Domain.Auth;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    [Fact]
    public void Should_Be_Active_When_Issued_And_Not_Expired()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);

        token.IsActive(IssuedAt.AddDays(6)).ShouldBeTrue();
        token.ExpiresAt.ShouldBe(IssuedAt.Add(Lifetime));
    }

    [Fact]
    public void Should_Not_Be_Active_When_Expired()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);

        token.IsActive(IssuedAt.Add(Lifetime)).ShouldBeFalse();
    }

    [Fact]
    public void Should_Revoke_Current_Token_And_Issue_Replacement_In_Same_Family_When_Rotated()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        var rotatedAt = IssuedAt.AddHours(1);

        var result = token.Rotate("hash-2", rotatedAt, Lifetime);

        result.IsError.ShouldBeFalse();
        var replacement = result.Value;
        token.RevokedAt.ShouldBe(rotatedAt);
        token.ReplacedByTokenId.ShouldBe(replacement.Id);
        replacement.FamilyId.ShouldBe(token.FamilyId);
        replacement.UserId.ShouldBe(UserId);
        replacement.TokenHash.ShouldBe("hash-2");
        replacement.IsActive(rotatedAt).ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Rotation_When_Token_Was_Already_Rotated()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        token.Rotate("hash-2", IssuedAt.AddHours(1), Lifetime);

        var result = token.Rotate("hash-3", IssuedAt.AddHours(2), Lifetime);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(RefreshTokenErrors.Reused);
    }

    [Fact]
    public void Should_Issue_Another_Replacement_In_Same_Family_When_Rotated_Token_Is_Reused_Within_Grace_Period()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        var rotatedAt = IssuedAt.AddHours(1);
        var firstReplacement = token.Rotate("hash-2", rotatedAt, Lifetime, GracePeriod).Value;

        var result = token.Rotate("hash-3", rotatedAt.AddSeconds(29), Lifetime, GracePeriod, familyIsActive: true);

        result.IsError.ShouldBeFalse();
        result.Value.FamilyId.ShouldBe(token.FamilyId);
        result.Value.TokenHash.ShouldBe("hash-3");
        result.Value.Id.ShouldNotBe(firstReplacement.Id);
        token.RevokedAt.ShouldBe(rotatedAt);
        token.ReplacedByTokenId.ShouldBe(firstReplacement.Id);
    }

    [Fact]
    public void Should_Reject_Rotation_As_Reused_When_Grace_Period_Has_Passed()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        var rotatedAt = IssuedAt.AddHours(1);
        token.Rotate("hash-2", rotatedAt, Lifetime, GracePeriod);

        var result = token.Rotate("hash-3", rotatedAt.Add(GracePeriod), Lifetime, GracePeriod, familyIsActive: true);

        result.FirstError.ShouldBe(RefreshTokenErrors.Reused);
    }

    [Fact]
    public void Should_Reject_Rotation_As_Reused_When_Family_Is_Not_Active_Within_Grace_Period()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        var rotatedAt = IssuedAt.AddHours(1);
        token.Rotate("hash-2", rotatedAt, Lifetime, GracePeriod);

        var result = token.Rotate("hash-3", rotatedAt.AddSeconds(1), Lifetime, GracePeriod, familyIsActive: false);

        result.FirstError.ShouldBe(RefreshTokenErrors.Reused);
    }

    [Fact]
    public void Should_Reject_Rotation_As_Reused_When_Grace_Period_Is_Zero()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        var rotatedAt = IssuedAt.AddHours(1);
        token.Rotate("hash-2", rotatedAt, Lifetime);

        var result = token.Rotate("hash-3", rotatedAt, Lifetime, TimeSpan.Zero, familyIsActive: true);

        result.FirstError.ShouldBe(RefreshTokenErrors.Reused);
    }

    [Fact]
    public void Should_Reject_Rotation_As_Revoked_When_Token_Was_Revoked_Without_Replacement_Within_Grace_Period()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        token.Revoke(IssuedAt.AddHours(1));

        var result = token.Rotate("hash-2", IssuedAt.AddHours(1).AddSeconds(1), Lifetime, GracePeriod, familyIsActive: true);

        result.FirstError.ShouldBe(RefreshTokenErrors.Revoked);
    }

    [Fact]
    public void Should_Reject_Rotation_As_Revoked_When_Token_Was_Revoked_Without_Replacement()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);
        token.Revoke(IssuedAt.AddHours(1));

        var result = token.Rotate("hash-2", IssuedAt.AddHours(2), Lifetime);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(RefreshTokenErrors.Revoked);
    }

    [Fact]
    public void Should_Reject_Rotation_When_Token_Is_Expired()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);

        var result = token.Rotate("hash-2", IssuedAt.Add(Lifetime), Lifetime);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(RefreshTokenErrors.Expired);
        token.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void Should_Keep_First_Revocation_Time_When_Revoked_Twice()
    {
        var token = RefreshToken.Issue(UserId, "hash-1", IssuedAt, Lifetime);

        token.Revoke(IssuedAt.AddHours(1));
        token.Revoke(IssuedAt.AddHours(2));

        token.RevokedAt.ShouldBe(IssuedAt.AddHours(1));
        token.IsActive(IssuedAt.AddHours(3)).ShouldBeFalse();
    }
}
