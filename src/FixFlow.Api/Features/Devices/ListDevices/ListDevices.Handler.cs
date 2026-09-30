using FixFlow.Api.Common.Caching;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FixFlow.Api.Features.Devices.ListDevices;

public sealed class ListDevicesHandler(FixFlowDbContext dbContext, HybridCache cache)
{
    public async Task<PagedResponse<DeviceListItemResponse>> HandleAsync(ListDevicesRequest request, CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();

        return await cache.GetOrCreateAsync(
            $"devices:list-items:{request.Page}:{request.PageSize}:{request.ClientId}:{search}",
            async token => await QueryAsync(request, search, token),
            tags: [CacheTags.Devices],
            cancellationToken: cancellationToken);
    }

    private Task<PagedResponse<DeviceListItemResponse>> QueryAsync(ListDevicesRequest request, string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Devices
            .AsNoTracking()
            .Where(device => device.ArchivedAt == null);

        if (request.ClientId is { } clientId)
        {
            query = query.Where(device => device.ClientId == clientId);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(device =>
                EF.Functions.ILike(device.SerialNumber, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(device.Model, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(device.Manufacturer, pattern, LikePattern.EscapeCharacter));
        }

        var rows =
            from device in query
            join client in dbContext.Clients on device.ClientId equals client.Id
            select new DeviceListRow { Device = device, ClientName = client.Name };

        return rows
            .OrderBy(row => row.Device.SerialNumber)
            .ToPagedResponseAsync(request, DeviceListItemResponse.FromRow, cancellationToken);
    }
}
