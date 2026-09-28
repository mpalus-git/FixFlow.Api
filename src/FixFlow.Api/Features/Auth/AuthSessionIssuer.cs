using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.Auth;

public sealed class AuthSessionIssuer(
    UserManager<ApplicationUser> userManager,
    FixFlowDbContext dbContext,
    AccessTokenIssuer accessTokenIssuer,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    public async Task<AuthTokensResponse> StartSessionAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var accessToken = accessTokenIssuer.Issue(user, await userManager.GetRolesAsync(user));
        var refreshTokenSecret = RefreshTokenSecret.Generate();
        var refreshToken = RefreshToken.Issue(
            user.Id,
            RefreshTokenSecret.Hash(refreshTokenSecret),
            timeProvider.GetUtcNow(),
            jwtOptions.Value.RefreshTokenLifetime);

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(accessToken.Value, accessToken.ExpiresAt, refreshTokenSecret, refreshToken.ExpiresAt);
    }
}
