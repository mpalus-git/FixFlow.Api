using System.Security.Claims;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<WorkOrderResponse>> HandleAsync(ListWorkOrdersRequest request, ClaimsPrincipal user, CancellationToken cancellationToken)
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

        return query
            .OrderBy(workOrder => workOrder.DueDate)
            .ThenBy(workOrder => workOrder.Id)
            .ToPagedResponseAsync(request, WorkOrderResponse.FromDomain, cancellationToken);
    }
}
