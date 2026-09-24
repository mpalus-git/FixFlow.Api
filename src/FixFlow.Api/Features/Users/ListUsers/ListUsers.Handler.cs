using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.ListUsers;

public sealed class ListUsersHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<UserResponse>> HandleAsync(ListUsersRequest request, CancellationToken cancellationToken)
    {
        var query =
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            select new { user.Id, Email = user.Email!, Role = role.Name! };

        if (request.Role is not null)
        {
            query = query.Where(user => user.Role == request.Role);
        }

        return query
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .ToPagedResponseAsync(request, user => new UserResponse(user.Id, user.Email, user.Role), cancellationToken);
    }
}
