using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.IntegrationTests.Persistence;

public sealed class IdentitySeederTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_Demo_Users_With_Roles_Once_When_Seeding_Is_Enabled_And_Run_Twice()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        (await userManager.Users.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(3);
        await ShouldBeInRoleWithPasswordAsync(userManager, DemoUsersOptions.AdminEmail, Roles.Admin);
        await ShouldBeInRoleWithPasswordAsync(userManager, DemoUsersOptions.DispatcherEmail, Roles.Dispatcher);
        await ShouldBeInRoleWithPasswordAsync(userManager, DemoUsersOptions.TechnicianEmail, Roles.Technician);
    }

    [Fact]
    public async Task Should_Create_Only_Roles_When_Demo_Users_Seeding_Is_Disabled()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seeder = new IdentitySeeder(roleManager, userManager, Options.Create(new DemoUsersOptions { Enabled = false }));

        await seeder.SeedAsync();

        (await userManager.Users.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        var roleNames = await roleManager.Roles.Select(role => role.Name).ToListAsync(TestContext.Current.CancellationToken);
        roleNames.ShouldBe(Roles.All, ignoreOrder: true);
    }

    private async Task ShouldBeInRoleWithPasswordAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        (await userManager.IsInRoleAsync(user, role)).ShouldBeTrue();
        (await userManager.CheckPasswordAsync(user, Factory.DemoUsersPassword)).ShouldBeTrue();
    }
}
