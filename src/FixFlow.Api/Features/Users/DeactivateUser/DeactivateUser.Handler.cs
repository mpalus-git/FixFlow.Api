using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.DeactivateUser;

public sealed class DeactivateUserHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid userId, ClaimsPrincipal administrator, CancellationToken cancellationToken)
    {
        if (userId == administrator.GetUserId())
        {
            return UserErrors.CannotDeactivateSelf;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        var hasOpenWorkOrders = await dbContext.WorkOrders.AnyAsync(
            workOrder => workOrder.TechnicianId == userId
                && (workOrder.Status == WorkOrderStatus.Assigned || workOrder.Status == WorkOrderStatus.InProgress),
            cancellationToken);
        if (hasOpenWorkOrders)
        {
            return UserErrors.HasOpenWorkOrders;
        }

        var now = timeProvider.GetUtcNow();
        user.Deactivate(now);
        await dbContext.RevokeActiveRefreshTokensOfUserAsync(userId, now, cancellationToken);

        return await dbContext.SaveChangesOrConflictAsync(cancellationToken);
    }
}
