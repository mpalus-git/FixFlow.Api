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

        var rotation = await RotateAsync(currentToken, now, afterConcurrentRotation: false, cancellationToken);
        if (rotation.IsError)
        {
            return rotation.Errors;
        }

        var accessToken = accessTokenIssuer.Issue(user, await userManager.GetRolesAsync(user));

        return new AuthTokensResponse(accessToken.Value, accessToken.ExpiresAt, rotation.Value.Secret, rotation.Value.Token.ExpiresAt);
    }

    private async Task<ErrorOr<IssuedRefreshToken>> RotateAsync(
        RefreshToken currentToken,
        DateTimeOffset now,
        bool afterConcurrentRotation,
        CancellationToken cancellationToken)
    {
        var options = jwtOptions.Value;
        var replacementSecret = RefreshTokenSecret.Generate();
        var familyIsActive = currentToken.IsRevoked && await dbContext.RefreshTokens.AnyAsync(
            token => token.FamilyId == currentToken.FamilyId && token.RevokedAt == null && token.ExpiresAt > now,
            cancellationToken);
        var rotation = currentToken.Rotate(
            RefreshTokenSecret.Hash(replacementSecret),
            now,
            options.RefreshTokenLifetime,
            options.RefreshTokenReuseGracePeriod,
            familyIsActive);
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
            return new IssuedRefreshToken(replacementSecret, rotation.Value);
        }
        catch (DbUpdateConcurrencyException) when (!afterConcurrentRotation)
        {
            dbContext.ChangeTracker.Clear();
            var rotatedToken = await dbContext.RefreshTokens.SingleAsync(token => token.Id == currentToken.Id, cancellationToken);
            return await RotateAsync(rotatedToken, now, afterConcurrentRotation: true, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await dbContext.RevokeRefreshTokenFamilyAsync(currentToken.FamilyId, now, cancellationToken);
            return RefreshTokenErrors.Reused;
        }
    }

    private sealed record IssuedRefreshToken(string Secret, RefreshToken Token);
}
