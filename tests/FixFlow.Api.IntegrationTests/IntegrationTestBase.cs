using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests;

public abstract class IntegrationTestBase(FixFlowApiFactory factory) : IAsyncLifetime
{
    protected FixFlowApiFactory Factory { get; } = factory;

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected async Task<TestUser> CreateUserAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@fixflow.test";
        var password = $"Tt1!{Guid.NewGuid():N}";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        (await userManager.CreateAsync(user, password)).Succeeded.ShouldBeTrue();
        (await userManager.AddToRoleAsync(user, role)).Succeeded.ShouldBeTrue();

        return new TestUser(user.Id, email, password);
    }
}

public sealed record TestUser(Guid Id, string Email, string Password);
