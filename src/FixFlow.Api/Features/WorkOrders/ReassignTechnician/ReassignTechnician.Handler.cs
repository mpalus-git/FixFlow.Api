using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.ReassignTechnician;

public sealed class ReassignTechnicianHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, ReassignTechnicianRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.FindRowAsync(workOrderId, dbContext, cancellationToken);
        if (row is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var workOrder = row.WorkOrder;

        var technician = await dbContext.FindActiveTechnicianAsync(request.TechnicianId, cancellationToken);
        if (technician is null)
        {
            return WorkOrderErrors.TechnicianNotFound;
        }

        var reassignment = workOrder.Reassign(request.TechnicianId, request.DueDate?.ToDatabasePrecision(), timeProvider.GetUtcNow(), actorId);
        if (reassignment.IsError)
        {
            return reassignment.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.VersionedResponse(row.WithTechnician(technician));
    }
}
