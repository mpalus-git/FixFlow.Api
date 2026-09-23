using FixFlow.Api.Common.Auth;

namespace FixFlow.Api.UnitTests.Common.Auth;

public sealed class RefreshTokenSecretTests
{
    [Fact]
    public void Should_Generate_Unique_Url_Safe_Secrets_When_Called_Repeatedly()
    {
        var secrets = Enumerable.Range(0, 100).Select(_ => RefreshTokenSecret.Generate()).ToList();

        secrets.Distinct().Count().ShouldBe(100);
        secrets.ShouldAllBe(secret => secret.Length >= 86 && !secret.Contains('+') && !secret.Contains('/') && !secret.Contains('='));
    }

    [Fact]
    public void Should_Return_Same_Sha256_Hex_Hash_When_Secret_Is_The_Same()
    {
        var secret = RefreshTokenSecret.Generate();

        var firstHash = RefreshTokenSecret.Hash(secret);
        var secondHash = RefreshTokenSecret.Hash(secret);

        firstHash.ShouldBe(secondHash);
        firstHash.Length.ShouldBe(64);
        firstHash.ShouldNotBe(secret);
    }
}
