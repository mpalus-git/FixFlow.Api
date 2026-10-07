using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Auth.Logout;

public sealed class LogoutHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task HandleAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenSecret.Hash(request.RefreshToken);
        var sessionToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (sessionToken is not null)
        {
            await dbContext.RevokeRefreshTokenFamilyAsync(sessionToken.FamilyId, timeProvider.GetUtcNow(), cancellationToken);
        }
    }
}
