using ErrorOr;
using FixFlow.Api.Common.Persistence;

namespace FixFlow.Api.Features.Users.GetUser;

public sealed class GetUserHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<UserResponse>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.FindUserResponseAsync(userId, cancellationToken);

        return user is null ? UserErrors.NotFound : user;
    }
}
