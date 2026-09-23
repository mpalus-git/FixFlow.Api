using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Clients.ArchiveClient;

public sealed class ArchiveClientHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        client.Archive(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
