using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Auth.DeleteExpiredRefreshTokens;

public sealed partial class DeleteExpiredRefreshTokensHandler(
    FixFlowDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DeleteExpiredRefreshTokensHandler> logger)
{
    public static readonly TimeSpan RetentionAfterFamilyExpiry = TimeSpan.FromDays(30);

    public async Task<int> HandleAsync(CancellationToken cancellationToken)
    {
        var retentionStart = timeProvider.GetUtcNow() - RetentionAfterFamilyExpiry;
        var deletedCount = await dbContext.RefreshTokens
            .Where(token => !dbContext.RefreshTokens.Any(familyToken => familyToken.FamilyId == token.FamilyId && familyToken.ExpiresAt >= retentionStart))
            .ExecuteDeleteAsync(cancellationToken);

        LogExpiredRefreshTokensDeleted(deletedCount);

        return deletedCount;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {DeletedCount} refresh tokens of expired families")]
    private partial void LogExpiredRefreshTokensDeleted(int deletedCount);
}
