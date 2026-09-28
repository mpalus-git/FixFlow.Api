using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Devices.ArchiveDevice;

public sealed class ArchiveDeviceHandler(FixFlowDbContext dbContext, TimeProvider timeProvider, HybridCache cache)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices.SingleOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
        if (device is null)
        {
            return DeviceErrors.NotFound;
        }

        device.Archive(timeProvider.GetUtcNow());
        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        await cache.RemoveByTagAsync(CacheTags.Devices, cancellationToken);

        return Result.Success;
    }
}
