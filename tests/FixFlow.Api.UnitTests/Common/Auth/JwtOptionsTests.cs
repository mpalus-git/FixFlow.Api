using FixFlow.Api.Common.Auth;

namespace FixFlow.Api.UnitTests.Common.Auth;

public sealed class JwtOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(300)]
    public void Should_Be_Valid_When_Refresh_Token_Reuse_Grace_Period_Is_Within_Five_Minutes(int seconds)
    {
        CreateOptions(TimeSpan.FromSeconds(seconds)).IsValid().ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(301)]
    public void Should_Be_Invalid_When_Refresh_Token_Reuse_Grace_Period_Is_Outside_Allowed_Range(int seconds)
    {
        CreateOptions(TimeSpan.FromSeconds(seconds)).IsValid().ShouldBeFalse();
    }

    private static JwtOptions CreateOptions(TimeSpan reuseGracePeriod) => new()
    {
        Issuer = "FixFlow.Api",
        Audience = "FixFlow.Clients",
        SigningKey = new string('k', 32),
        RefreshTokenReuseGracePeriod = reuseGracePeriod,
    };
}
