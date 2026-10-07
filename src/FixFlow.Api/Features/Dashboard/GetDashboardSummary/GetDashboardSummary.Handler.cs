using System.Data;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Dashboard.GetDashboardSummary;

public sealed class GetDashboardSummaryHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly WorkOrderStatus[] LifecycleStatuses =
    [
        WorkOrderStatus.New,
        WorkOrderStatus.Assigned,
        WorkOrderStatus.InProgress,
        WorkOrderStatus.Completed,
        WorkOrderStatus.Invoiced,
    ];

    public Task<DashboardSummaryResponse> HandleAsync(CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(timeProvider.GetUtcNow(), SummarizeInSnapshotAsync, cancellationToken);

    private async Task<DashboardSummaryResponse> SummarizeInSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var weekStart = BusinessTime.StartOfWeek(now);
        var nextWeekStart = weekStart.AddDays(7);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", cancellationToken);

        var statusCounts = await CountWorkOrdersByStatusAsync(cancellationToken);
        var overdueCount = await dbContext.WorkOrders.CountAsync(workOrder => workOrder.IsOverdue, cancellationToken);
        var technicians = await GetTechnicianWorkloadsAsync(
            BusinessTime.StartOfDay(weekStart),
            BusinessTime.StartOfDay(nextWeekStart),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DashboardSummaryResponse(now, weekStart, nextWeekStart.AddDays(-1), statusCounts, overdueCount, technicians);
    }

    private async Task<List<WorkOrderStatusCountResponse>> CountWorkOrdersByStatusAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext.WorkOrders.CountByStatusAsync(cancellationToken);

        return [.. LifecycleStatuses.Select(status => new WorkOrderStatusCountResponse(status, counts.GetValueOrDefault(status)))];
    }

    private async Task<List<TechnicianWorkloadResponse>> GetTechnicianWorkloadsAsync(
        DateTimeOffset weekStart,
        DateTimeOffset nextWeekStart,
        CancellationToken cancellationToken)
    {
        var technicians = await dbContext.UsersWithRoles()
            .Where(candidate => candidate.RoleName == Roles.Technician && candidate.User.DeactivatedAt == null && candidate.User.Email != null)
            .OrderBy(candidate => candidate.User.FullName)
            .ThenBy(candidate => candidate.User.Email)
            .Select(candidate => new { candidate.User.Id, Email = candidate.User.Email!, candidate.User.FullName })
            .ToListAsync(cancellationToken);

        var workloads = await dbContext.WorkOrders
            .AsNoTracking()
            .Where(workOrder => workOrder.TechnicianId != null
                && (workOrder.Status == WorkOrderStatus.Assigned || workOrder.Status == WorkOrderStatus.InProgress))
            .GroupBy(workOrder => workOrder.TechnicianId!.Value)
            .Select(group => new
            {
                TechnicianId = group.Key,
                AssignedCount = group.Count(workOrder => workOrder.Status == WorkOrderStatus.Assigned),
                InProgressCount = group.Count(workOrder => workOrder.Status == WorkOrderStatus.InProgress),
                OverdueCount = group.Count(workOrder => workOrder.IsOverdue),
                DueThisWeekCount = group.Count(workOrder => workOrder.DueDate >= weekStart && workOrder.DueDate < nextWeekStart),
            })
            .ToDictionaryAsync(workload => workload.TechnicianId, cancellationToken);

        return technicians.ConvertAll(technician => workloads.TryGetValue(technician.Id, out var workload)
            ? new TechnicianWorkloadResponse(
                technician.Id,
                technician.Email,
                technician.FullName,
                workload.AssignedCount,
                workload.InProgressCount,
                workload.OverdueCount,
                workload.DueThisWeekCount)
            : new TechnicianWorkloadResponse(technician.Id, technician.Email, technician.FullName, 0, 0, 0, 0));
    }
}
