using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.MarkOverdueWorkOrders;

public sealed partial class MarkOverdueWorkOrdersHandler(
    FixFlowDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<MarkOverdueWorkOrdersHandler> logger)
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken)
    {
        var markedCount = await dbContext.WorkOrders
            .Where(WorkOrder.IsPastDueAt(timeProvider.GetUtcNow()))
            .Where(workOrder => !workOrder.IsOverdue)
            .ExecuteUpdateAsync(setters => setters.SetProperty(workOrder => workOrder.IsOverdue, true), cancellationToken);

        LogOverdueWorkOrdersMarked(markedCount);

        return markedCount;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Marked {MarkedCount} work orders as overdue")]
    private partial void LogOverdueWorkOrdersMarked(int markedCount);
}
