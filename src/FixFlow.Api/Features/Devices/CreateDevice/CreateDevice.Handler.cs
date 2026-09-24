using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Devices.CreateDevice;

public sealed class CreateDeviceHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<DeviceResponse>> HandleAsync(CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(client => client.Id == request.ClientId, cancellationToken);
        if (client is null)
        {
            return ClientErrors.NotFound;
        }

        var creation = Device.Create(
            client,
            request.SerialNumber,
            request.Model,
            request.Manufacturer,
            request.InstallationDate,
            timeProvider.GetUtcNow());
        if (creation.IsError)
        {
            return creation.Errors;
        }

        dbContext.Devices.Add(creation.Value);
        var saving = await dbContext.SaveChangesOrConflictAsync(
            DeviceConfiguration.SerialNumberIndexName,
            DeviceErrors.DuplicateSerialNumber,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return DeviceResponse.FromDomain(creation.Value);
    }
}
