using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.AssignTechnician;

public sealed class AssignTechnicianHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, AssignTechnicianRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders.SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        if (!await dbContext.IsActiveTechnicianAsync(request.TechnicianId, cancellationToken))
        {
            return WorkOrderErrors.TechnicianNotFound;
        }

        var assignment = workOrder.Assign(request.TechnicianId, request.DueDate?.ToDatabasePrecision(), timeProvider.GetUtcNow());
        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return await dbContext.VersionedWorkOrderResponseAsync(workOrder, cancellationToken);
    }
}
