using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.Auth.Refresh;

public sealed class RefreshHandler(
    UserManager<ApplicationUser> userManager,
    FixFlowDbContext dbContext,
    AccessTokenIssuer accessTokenIssuer,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
{
    public async Task<ErrorOr<AuthTokensResponse>> HandleAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = RefreshTokenSecret.Hash(request.RefreshToken);
        var currentToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (currentToken is null)
        {
            return RefreshTokenErrors.Invalid;
        }

        var user = await userManager.FindByIdAsync(currentToken.UserId.ToString());
        if (user is null)
        {
            return RefreshTokenErrors.Invalid;
        }

        if (!user.IsActive)
        {
            await dbContext.RevokeRefreshTokenFamilyAsync(currentToken.FamilyId, now, cancellationToken);
            return RefreshTokenErrors.Invalid;
        }

        var replacementSecret = RefreshTokenSecret.Generate();
        var rotation = currentToken.Rotate(RefreshTokenSecret.Hash(replacementSecret), now, jwtOptions.Value.RefreshTokenLifetime);
        if (rotation.IsError)
        {
            if (rotation.FirstError == RefreshTokenErrors.Reused)
            {
                await dbContext.RevokeRefreshTokenFamilyAsync(currentToken.FamilyId, now, cancellationToken);
            }

            return rotation.Errors;
        }

        dbContext.RefreshTokens.Add(rotation.Value);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await dbContext.RevokeRefreshTokenFamilyAsync(currentToken.FamilyId, now, cancellationToken);
            return RefreshTokenErrors.Reused;
        }

        var accessToken = accessTokenIssuer.Issue(user, await userManager.GetRolesAsync(user));

        return new AuthTokensResponse(accessToken.Value, accessToken.ExpiresAt, replacementSecret, rotation.Value.ExpiresAt);
    }
}
