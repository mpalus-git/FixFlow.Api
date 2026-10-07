using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.WorkOrders;

public sealed record ActiveTechnician(string? Email, string FullName);

public static class ActiveTechnicians
{
    public static Task<ActiveTechnician?> FindActiveTechnicianAsync(this FixFlowDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.UsersWithRoles()
            .Where(candidate => candidate.User.Id == userId && candidate.User.DeactivatedAt == null && candidate.RoleName == Roles.Technician)
            .Select(candidate => new ActiveTechnician(candidate.User.Email, candidate.User.FullName))
            .SingleOrDefaultAsync(cancellationToken);
}
