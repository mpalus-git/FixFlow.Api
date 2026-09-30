using System.Security.Claims;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<WorkOrderListItemResponse>> HandleAsync(ListWorkOrdersRequest request, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var query = dbContext.WorkOrders
            .AsNoTracking()
            .VisibleTo(user);

        if (request.Status is { } status)
        {
            query = query.Where(workOrder => workOrder.Status == status);
        }

        if (request.TechnicianId is { } technicianId)
        {
            query = query.Where(workOrder => workOrder.TechnicianId == technicianId);
        }

        if (request.DeviceId is { } deviceId)
        {
            query = query.Where(workOrder => workOrder.DeviceId == deviceId);
        }

        if (request.IsOverdue is { } isOverdue)
        {
            query = query.Where(workOrder => workOrder.IsOverdue == isOverdue);
        }

        if (request.DueFrom is { } dueFrom)
        {
            var rangeStart = BusinessTime.StartOfDay(dueFrom);
            query = query.Where(workOrder => workOrder.DueDate >= rangeStart);
        }

        if (request.DueTo is { } dueTo)
        {
            var rangeEnd = BusinessTime.StartOfDay(dueTo.AddDays(1));
            query = query.Where(workOrder => workOrder.DueDate < rangeEnd);
        }

        var rows =
            from workOrder in query
            join device in dbContext.Devices on workOrder.DeviceId equals device.Id
            join client in dbContext.Clients on device.ClientId equals client.Id
            join technician in dbContext.Users on workOrder.TechnicianId equals technician.Id into technicians
            from technician in technicians.DefaultIfEmpty()
            select new WorkOrderListRow
            {
                WorkOrder = workOrder,
                DeviceSerialNumber = device.SerialNumber,
                DeviceModel = device.Model,
                ClientId = client.Id,
                ClientName = client.Name,
                TechnicianEmail = technician == null ? null : technician.Email,
            };

        if (request.ClientId is { } clientId)
        {
            rows = rows.Where(row => row.ClientId == clientId);
        }

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            rows = rows.Where(row =>
                EF.Functions.ILike(row.WorkOrder.Description, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(row.DeviceSerialNumber, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(row.ClientName, pattern, LikePattern.EscapeCharacter));
        }

        return rows
            .OrderBy(row => row.WorkOrder.DueDate)
            .ThenBy(row => row.WorkOrder.Id)
            .ToPagedResponseAsync(request, WorkOrderListItemResponse.FromRow, cancellationToken);
    }
}
