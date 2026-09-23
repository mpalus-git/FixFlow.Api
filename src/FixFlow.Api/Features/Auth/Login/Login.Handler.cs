using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.Auth.Login;

public sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    FixFlowDbContext dbContext,
    AccessTokenIssuer accessTokenIssuer,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    public async Task<ErrorOr<AuthTokensResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

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
