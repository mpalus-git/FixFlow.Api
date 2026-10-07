using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.ListWorkOrderEvents;

public sealed class ListWorkOrderEventsHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<List<WorkOrderEventResponse>>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrderIsVisible = await dbContext.WorkOrders
            .VisibleTo(user)
            .AnyAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (!workOrderIsVisible)
        {
            return WorkOrderErrors.NotFound;
        }

        return await (
            from workOrderEvent in dbContext.WorkOrderEvents.AsNoTracking()
            where workOrderEvent.WorkOrderId == workOrderId
            join actor in dbContext.Users on workOrderEvent.ActorId equals (Guid?)actor.Id into actors
            from actor in actors.DefaultIfEmpty()
            join technician in dbContext.Users on workOrderEvent.TechnicianId equals (Guid?)technician.Id into technicians
            from technician in technicians.DefaultIfEmpty()
            orderby workOrderEvent.OccurredAt, workOrderEvent.Id
            select new WorkOrderEventResponse(
                workOrderEvent.Id,
                workOrderEvent.Type,
                workOrderEvent.OccurredAt,
                workOrderEvent.ActorId,
                actor == null ? null : actor.FullName,
                workOrderEvent.TechnicianId,
                technician == null ? null : technician.FullName,
                workOrderEvent.DueDate))
            .ToListAsync(cancellationToken);
    }
}
