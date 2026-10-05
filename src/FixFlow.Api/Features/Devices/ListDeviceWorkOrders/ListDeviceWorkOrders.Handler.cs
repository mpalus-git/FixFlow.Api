using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Devices.ListDeviceWorkOrders;

public sealed class ListDeviceWorkOrdersHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<PagedResponse<DeviceWorkOrderHistoryItemResponse>>> HandleAsync(
        Guid deviceId,
        ListDeviceWorkOrdersRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var workOrders = dbContext.WorkOrders
            .AsNoTracking()
            .VisibleTo(user)
            .Where(workOrder => workOrder.DeviceId == deviceId);

        var deviceIsVisible = user.IsInRole(Roles.Technician)
            ? await workOrders.AnyAsync(cancellationToken)
            : await dbContext.Devices.AnyAsync(device => device.Id == deviceId, cancellationToken);
        if (!deviceIsVisible)
        {
            return DeviceErrors.NotFound;
        }

        var rows =
            from workOrder in workOrders
            join technician in dbContext.Users on workOrder.TechnicianId equals technician.Id into technicians
            from technician in technicians.DefaultIfEmpty()
            select new DeviceWorkOrderHistoryRow
            {
                WorkOrder = workOrder,
                TechnicianName = technician == null ? null : technician.FullName,
                ServiceEntryCount = dbContext.ServiceEntries.Count(entry => entry.WorkOrderId == workOrder.Id),
            };

        return await rows
            .OrderByDescending(row => row.WorkOrder.CreatedAt)
            .ThenByDescending(row => row.WorkOrder.Id)
            .ToPagedResponseAsync(request, DeviceWorkOrderHistoryItemResponse.FromRow, cancellationToken);
    }
}
