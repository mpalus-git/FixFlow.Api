using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

public sealed class UpdateWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(
        Guid workOrderId,
        UpdateWorkOrderRequest request,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.FindRowAsync(workOrderId, dbContext, cancellationToken);
        if (row is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var workOrder = row.WorkOrder;

        var precondition = dbContext.EnsureVersionMatches(workOrder, ifMatch);
        if (precondition.IsError)
        {
            return precondition.Errors;
        }

        var update = workOrder.Update(
            request.Description,
            request.Priority,
            request.DueDate.ToDatabasePrecision(),
            timeProvider.GetUtcNow());
        if (update.IsError)
        {
            return update.Errors;
        }

        var saving = await dbContext.SaveChangesOrPreconditionFailedAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.VersionedResponse(row);
    }
}
