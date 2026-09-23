using FixFlow.Api.Common.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixFlow.Api.UnitTests.Common.Auth;

public sealed class AccessTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly JwtOptions _options = new()
    {
        Issuer = "FixFlow.Tests",
        Audience = "FixFlow.Tests.Clients",
        SigningKey = new string('k', 48),
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
    };

    private readonly ApplicationUser _user = new()
    {
        Id = Guid.CreateVersion7(),
        Email = "technician@fixflow.test",
    };

    [Fact]
    public void Should_Issue_Token_With_User_Claims_And_Roles_When_User_Has_Roles()
    {
        var issuer = new AccessTokenIssuer(Options.Create(_options), new FakeTimeProvider(Now));

        var accessToken = issuer.Issue(_user, [Roles.Dispatcher, Roles.Technician]);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value);
        token.Subject.ShouldBe(_user.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(_user.Email);
        token.Claims.Where(claim => claim.Type == AuthClaimTypes.Role).Select(claim => claim.Value)
            .ShouldBe([Roles.Dispatcher, Roles.Technician], ignoreOrder: true);
        token.Issuer.ShouldBe(_options.Issuer);
        token.Audiences.ShouldBe([_options.Audience]);
    }

    [Fact]
    public void Should_Expire_Token_After_Configured_Lifetime_When_Issued()
    {
        var issuer = new AccessTokenIssuer(Options.Create(_options), new FakeTimeProvider(Now));

        var accessToken = issuer.Issue(_user, []);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value);
        accessToken.ExpiresAt.ShouldBe(Now.AddMinutes(15));
        token.ValidTo.ShouldBe(Now.AddMinutes(15).UtcDateTime);
        token.IssuedAt.ShouldBe(Now.UtcDateTime);
    }

    [Fact]
    public async Task Should_Sign_Token_With_Configured_Key_When_Issued()
    {
        var issuer = new AccessTokenIssuer(Options.Create(_options), new FakeTimeProvider(Now));
        var accessToken = issuer.Issue(_user, []);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken.Value, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = AccessTokenIssuer.CreateSigningKey(_options.SigningKey),
            ValidateLifetime = false,
        });

        result.IsValid.ShouldBeTrue();
    }
}
