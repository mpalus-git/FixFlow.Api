using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;

namespace FixFlow.Api.Features.Users.ListUsers;

public sealed class ListUsersHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<UserResponse>> HandleAsync(ListUsersRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.UsersWithRoles()
            .Select(candidate => new
            {
                candidate.User.Id,
                Email = candidate.User.Email!,
                candidate.User.FullName,
                Role = candidate.RoleName,
                IsActive = candidate.User.DeactivatedAt == null,
            });

        if (request.Role is not null)
        {
            query = query.Where(user => user.Role == request.Role);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        return query
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .ThenBy(user => user.Id)
            .ToPagedResponseAsync(request, user => new UserResponse(user.Id, user.Email, user.FullName, user.Role, user.IsActive), cancellationToken);
    }
}
