using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;

public sealed class UpdateWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(
        Guid workOrderId,
        UpdateWorkOrderRequest request,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

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

        return await dbContext.VersionedWorkOrderResponseAsync(workOrder, cancellationToken);
    }
}
