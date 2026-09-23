using System.Security.Claims;
using System.Text;
using FixFlow.Api.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FixFlow.Api.Common.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed class AccessTokenIssuer(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Issue(ApplicationUser user, IEnumerable<string> roles)
    {
        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(options.AccessTokenLifetime);

        List<Claim> claims =
        [
            new(AuthClaimTypes.UserId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        ];
        claims.AddRange(roles.Select(role => new Claim(AuthClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSigningKey(options.SigningKey), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));
}
