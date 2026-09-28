using ErrorOr;
using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Devices.UpdateDevice;

public sealed class UpdateDeviceHandler(FixFlowDbContext dbContext, HybridCache cache)
{
    public async Task<ErrorOr<Versioned<DeviceResponse>>> HandleAsync(
        Guid deviceId,
        UpdateDeviceRequest request,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices.SingleOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
        if (device is null)
        {
            return DeviceErrors.NotFound;
        }

        var precondition = dbContext.EnsureVersionMatches(device, ifMatch);
        if (precondition.IsError)
        {
            return precondition.Errors;
        }

        var update = device.Update(request.SerialNumber, request.Model, request.Manufacturer, request.InstallationDate);
        if (update.IsError)
        {
            return update.Errors;
        }

        var saving = await dbContext.SaveChangesOrPreconditionFailedAsync(
            DeviceConfiguration.SerialNumberIndexName,
            DeviceErrors.DuplicateSerialNumber,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        await cache.RemoveByTagAsync(CacheTags.Devices, cancellationToken);

        return dbContext.Versioned(device, DeviceResponse.FromDomain(device));
    }
}
