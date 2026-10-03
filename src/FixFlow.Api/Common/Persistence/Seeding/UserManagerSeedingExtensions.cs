using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FixFlow.Api.Common.Persistence.Seeding;

public static class UserManagerSeedingExtensions
{
    public static async Task EnsureFullNameAsync(this UserManager<ApplicationUser> userManager, ApplicationUser user, string fullName)
    {
        if (user.FullName == fullName)
        {
            return;
        }

        user.ChangeFullName(fullName);
        (await userManager.UpdateAsync(user)).ThrowIfFailed(user.Email ?? user.Id.ToString());
    }
}
