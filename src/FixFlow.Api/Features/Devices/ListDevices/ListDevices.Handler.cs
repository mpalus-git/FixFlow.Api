using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Devices.ListDevices;

public sealed class ListDevicesHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<DeviceResponse>> HandleAsync(ListDevicesRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Devices
            .AsNoTracking()
            .Where(device => device.ArchivedAt == null);

        if (request.ClientId is { } clientId)
        {
            query = query.Where(device => device.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = LikePattern.Contains(request.Search.Trim());
            query = query.Where(device =>
                EF.Functions.ILike(device.SerialNumber, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(device.Model, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(device.Manufacturer, pattern, LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(device => device.SerialNumber)
            .ToPagedResponseAsync(request, DeviceResponse.FromDomain, cancellationToken);
    }
}
