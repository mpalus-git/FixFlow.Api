using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders;

public static class WorkOrderStatusCounts
{
    public static Task<Dictionary<WorkOrderStatus, int>> CountByStatusAsync(this IQueryable<WorkOrder> workOrders, CancellationToken cancellationToken) =>
        workOrders
            .GroupBy(workOrder => workOrder.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
}
