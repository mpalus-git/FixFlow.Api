using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.DemoData;
using FixFlow.Api.Features.DemoData.ResetDemoData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.DemoData;

public sealed class ResetDemoDataTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Restore_Demo_Data_When_Demo_Data_Is_Reset()
    {
        await using var demoFactory = CreateFactoryWithDemoData();
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountCreatedByAdmin = await CreateUserAsync(Roles.Technician);
        List<Guid> seededClientIds;
        await using (var scope = demoFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            seededClientIds = await dbContext.Clients.Select(client => client.Id).ToListAsync(cancellationToken);
            dbContext.Clients.Add(Client.Create("Visitor client", new Address("Street", "1", "00-001", "City"), "Contact", "123456789", null, DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var result = await ResetDemoDataAsync(demoFactory);

        result.IsError.ShouldBeFalse();
        await using var verificationScope = demoFactory.Services.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var clientIds = await verificationDbContext.Clients.Select(client => client.Id).ToListAsync(cancellationToken);
        clientIds.Count.ShouldBe(6);
        clientIds.ShouldNotContain(clientId => seededClientIds.Contains(clientId));
        (await verificationDbContext.WorkOrders.CountAsync(cancellationToken)).ShouldBe(18);
        (await verificationDbContext.Parts.CountAsync(cancellationToken)).ShouldBe(11);
        (await verificationDbContext.Users.AnyAsync(user => user.Id == accountCreatedByAdmin.Id, cancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Restore_Demo_Account_Access_When_Demo_Data_Is_Reset()
    {
        await using var demoFactory = CreateFactoryWithDemoData();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var scope = demoFactory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var technician = (await userManager.FindByEmailAsync(DemoUsersOptions.TechnicianEmail)).ShouldNotBeNull();
            (await userManager.ChangePasswordAsync(technician, Factory.DemoUsersPassword, $"Ch1!{Guid.NewGuid():N}")).Succeeded.ShouldBeTrue();
            (await userManager.SetLockoutEndDateAsync(technician, DateTimeOffset.UtcNow.AddHours(1))).Succeeded.ShouldBeTrue();
            technician.Deactivate(DateTimeOffset.UtcNow);
            (await userManager.UpdateAsync(technician)).Succeeded.ShouldBeTrue();
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            dbContext.RefreshTokens.Add(RefreshToken.Issue(technician.Id, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromDays(7)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var result = await ResetDemoDataAsync(demoFactory);

        result.IsError.ShouldBeFalse();
        await using var verificationScope = demoFactory.Services.CreateAsyncScope();
        var verificationUserManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var restoredTechnician = (await verificationUserManager.FindByEmailAsync(DemoUsersOptions.TechnicianEmail)).ShouldNotBeNull();
        (await verificationUserManager.CheckPasswordAsync(restoredTechnician, Factory.DemoUsersPassword)).ShouldBeTrue();
        (await verificationUserManager.IsLockedOutAsync(restoredTechnician)).ShouldBeFalse();
        restoredTechnician.IsActive.ShouldBeTrue();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await verificationDbContext.RefreshTokens.AnyAsync(token => token.UserId == restoredTechnician.Id && token.RevokedAt == null, cancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Return_Disabled_Error_When_Demo_Data_Is_Disabled()
    {
        var result = await ResetDemoDataAsync(Factory);

        result.FirstError.ShouldBe(DemoDataErrors.Disabled);
    }

    private WebApplicationFactory<Program> CreateFactoryWithDemoData() =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting("Seed:DemoData:Enabled", "true"));

    private static async Task<ErrorOr<Success>> ResetDemoDataAsync(WebApplicationFactory<Program> targetFactory)
    {
        await using var scope = targetFactory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ResetDemoDataHandler>().HandleAsync(TestContext.Current.CancellationToken);
    }
}
