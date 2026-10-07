using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.Common.Persistence;

public sealed class UserWithRole
{
    public required ApplicationUser User { get; init; }

    public required string RoleName { get; init; }
}

public static class UserWithRoleQueries
{
    public static IQueryable<UserWithRole> UsersWithRoles(this FixFlowDbContext dbContext) =>
        from user in dbContext.Users
        join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
        join role in dbContext.Roles on userRole.RoleId equals role.Id
        select new UserWithRole { User = user, RoleName = role.Name! };
}
