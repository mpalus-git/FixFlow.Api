using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.GetCurrentUser;

public sealed class GetCurrentUserHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<UserResponse>> HandleAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var currentUser = await (
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where user.Id == userId
            select new UserResponse(user.Id, user.Email!, role.Name!))
            .SingleOrDefaultAsync(cancellationToken);

        return currentUser is null ? UserErrors.NotFound : currentUser;
    }
}
