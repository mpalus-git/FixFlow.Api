using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.WorkOrders.StartWork;

public sealed class StartWorkHandler(FixFlowDbContext dbContext, TimeProvider timeProvider, IOptions<ClientClockOptions> clientClockOptions)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(
        Guid workOrderId,
        StartWorkRequest? request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.WorkOrders
            .VisibleTo(user)
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
        if (workOrder is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var technicianId = user.GetUserId();
        var technicianHasWorkInProgress = await dbContext.WorkOrders.AnyAsync(
            other => other.TechnicianId == technicianId && other.Status == WorkOrderStatus.InProgress && other.Id != workOrderId,
            cancellationToken);

        var start = workOrder.Start(
            technicianId,
            technicianHasWorkInProgress,
            timeProvider.GetUtcNow(),
            request?.StartedAt?.ToDatabasePrecision(),
            clientClockOptions.Value.MaxSkew);
        if (start.IsError)
        {
            return start.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(
            WorkOrderConfiguration.TechnicianInProgressIndexName,
            WorkOrderErrors.TechnicianAlreadyHasWorkInProgress,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return await dbContext.VersionedWorkOrderResponseAsync(workOrder, cancellationToken);
    }
}
