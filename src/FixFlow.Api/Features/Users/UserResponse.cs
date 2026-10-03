using System.ComponentModel;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Users;

[Description("User account.")]
public sealed record UserResponse(
    [property: Description("Identifier of the user account.")] Guid Id,
    [property: Description("Email address of the user account.")] string Email,
    [property: Description("First and last name of the user shown in clients instead of the email.")] string FullName,
    [property: Description("Role of the user account.")] string Role,
    [property: Description("False when an administrator deactivated the account; a deactivated account cannot sign in.")] bool IsActive);

public static class UserResponses
{
    public static Task<UserResponse?> FindUserResponseAsync(this FixFlowDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        (from user in dbContext.Users.AsNoTracking()
         join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
         join role in dbContext.Roles on userRole.RoleId equals role.Id
         where user.Id == userId
         select new UserResponse(user.Id, user.Email!, user.FullName, role.Name!, user.DeactivatedAt == null))
            .SingleOrDefaultAsync(cancellationToken);
}
