using ErrorOr;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users.ActivateUser;

public sealed class ActivateUserHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        user.Activate();

        return await dbContext.SaveChangesOrConflictAsync(cancellationToken);
    }
}
