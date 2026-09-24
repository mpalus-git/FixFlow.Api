using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders.GetWorkOrder;

public sealed class GetWorkOrderHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<WorkOrderResponse>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .AsNoTracking()
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);

        return workOrder is null ? WorkOrderErrors.NotFound : WorkOrderResponse.FromDomain(workOrder);
    }
}
