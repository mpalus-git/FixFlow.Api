using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;

namespace FixFlow.Api.Features.Users.GetCurrentUser;

public sealed class GetCurrentUserHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<UserResponse>> HandleAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUser = await dbContext.FindUserResponseAsync(principal.GetUserId(), cancellationToken);

        return currentUser is null ? UserErrors.NotFound : currentUser;
    }
}
