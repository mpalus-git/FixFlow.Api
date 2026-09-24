using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Devices.GetDevice;

public sealed class GetDeviceHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<DeviceResponse>> HandleAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices
            .AsNoTracking()
            .SingleOrDefaultAsync(device => device.Id == deviceId, cancellationToken);

        return device is null ? DeviceErrors.NotFound : DeviceResponse.FromDomain(device);
    }
}
