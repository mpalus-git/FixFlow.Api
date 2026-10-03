using ErrorOr;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.UpdateUser;

public sealed class UpdateUserHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<UserResponse>> HandleAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        user.ChangeFullName(request.FullName);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.FindUserResponseAsync(userId, cancellationToken);
        return response is null ? UserErrors.NotFound : response;
    }
}
