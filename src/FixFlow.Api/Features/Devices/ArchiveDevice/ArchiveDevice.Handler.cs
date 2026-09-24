using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Devices.ArchiveDevice;

public sealed class ArchiveDeviceHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices.SingleOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
        if (device is null)
        {
            return DeviceErrors.NotFound;
        }

        device.Archive(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
