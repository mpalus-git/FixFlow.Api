using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Clients.ArchiveClient;

public sealed class ArchiveClientHandler(FixFlowDbContext dbContext, TimeProvider timeProvider, HybridCache cache)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.SingleOrDefaultAsync(client => client.Id == clientId, cancellationToken);
        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        client.Archive(now);

        var activeDevices = await dbContext.Devices
            .Where(device => device.ClientId == clientId && device.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var device in activeDevices)
        {
            device.Archive(now);
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        await cache.RemoveByTagAsync([CacheTags.Clients, CacheTags.Devices], cancellationToken);

        return Result.Success;
    }
}
