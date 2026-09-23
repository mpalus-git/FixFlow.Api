using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Auth;

public static class RefreshTokenFamilyExtensions
{
    public static async Task RevokeRefreshTokenFamilyAsync(
        this FixFlowDbContext dbContext,
        Guid familyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var activeFamilyTokens = await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeFamilyTokens)
        {
            token.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
