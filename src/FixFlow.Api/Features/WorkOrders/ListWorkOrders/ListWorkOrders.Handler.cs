using System.Linq.Expressions;
using System.Security.Claims;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrders;

public sealed class ListWorkOrdersHandler(FixFlowDbContext dbContext)
{
    private static readonly Expression<Func<WorkOrderRow, int>> PriorityWeight = row =>
        row.WorkOrder.Priority == WorkOrderPriority.Low ? 0
        : row.WorkOrder.Priority == WorkOrderPriority.Normal ? 1
        : row.WorkOrder.Priority == WorkOrderPriority.High ? 2
        : 3;

    private static readonly Expression<Func<WorkOrderRow, int>> StatusLifecycleOrder = row =>
        row.WorkOrder.Status == WorkOrderStatus.New ? 0
        : row.WorkOrder.Status == WorkOrderStatus.Assigned ? 1
        : row.WorkOrder.Status == WorkOrderStatus.InProgress ? 2
        : row.WorkOrder.Status == WorkOrderStatus.Completed ? 3
        : 4;

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

        var rows = query.WithRelatedDetails(dbContext);

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

        return Sort(rows, Enum.Parse<WorkOrderSortField>(request.SortBy), Enum.Parse<SortDirection>(request.SortDirection))
            .ToPagedResponseAsync(request, WorkOrderListItemResponse.FromRow, cancellationToken);
    }

    private static IOrderedQueryable<WorkOrderRow> Sort(IQueryable<WorkOrderRow> rows, WorkOrderSortField sortField, SortDirection direction) =>
        sortField switch
        {
            WorkOrderSortField.CreatedAt => OrderByThenById(rows, row => row.WorkOrder.CreatedAt, direction),
            WorkOrderSortField.Priority => OrderByThenById(rows, PriorityWeight, direction),
            WorkOrderSortField.Status => OrderByThenById(rows, StatusLifecycleOrder, direction),
            WorkOrderSortField.ClientName => OrderByThenById(rows, row => row.ClientName, direction),
            _ => OrderByThenById(rows, row => row.WorkOrder.DueDate, direction),
        };

    private static IOrderedQueryable<WorkOrderRow> OrderByThenById<TKey>(IQueryable<WorkOrderRow> rows, Expression<Func<WorkOrderRow, TKey>> key, SortDirection direction) =>
        direction == SortDirection.Desc
            ? rows.OrderByDescending(key).ThenByDescending(row => row.WorkOrder.Id)
            : rows.OrderBy(key).ThenBy(row => row.WorkOrder.Id);
}
