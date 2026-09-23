using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<DemoUsersOptions> demoUsersOptions)
{
    public async Task SeedAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(role)), role);
            }
        }

        var demoUsers = demoUsersOptions.Value;
        if (!demoUsers.Enabled)
        {
            return;
        }

        await EnsureDemoUserAsync(DemoUsersOptions.AdminEmail, demoUsers.AdminPassword, Roles.Admin);
        await EnsureDemoUserAsync(DemoUsersOptions.DispatcherEmail, demoUsers.DispatcherPassword, Roles.Dispatcher);
        await EnsureDemoUserAsync(DemoUsersOptions.TechnicianEmail, demoUsers.TechnicianPassword, Roles.Technician);
    }

    private async Task EnsureDemoUserAsync(string email, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        EnsureSucceeded(await userManager.CreateAsync(user, password), email);
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role), email);
    }

    private static void EnsureSucceeded(IdentityResult result, string subject)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seeding '{subject}' failed: {errors}");
        }
    }
}
