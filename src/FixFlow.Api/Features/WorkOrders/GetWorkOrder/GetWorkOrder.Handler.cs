using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.GetWorkOrder;

public sealed class GetWorkOrderHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(Guid workOrderId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.VisibleTo(user).FindRowAsync(workOrderId, dbContext, cancellationToken);

        return row is null ? WorkOrderErrors.NotFound : dbContext.VersionedResponse(row);
    }
}
