using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders;

public static class ActiveTechnicians
{
    public static Task<bool> IsActiveTechnicianAsync(this FixFlowDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        (from user in dbContext.Users
         join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
         join role in dbContext.Roles on userRole.RoleId equals role.Id
         where user.Id == userId && user.DeactivatedAt == null && role.Name == Roles.Technician
         select userRole).AnyAsync(cancellationToken);
}
