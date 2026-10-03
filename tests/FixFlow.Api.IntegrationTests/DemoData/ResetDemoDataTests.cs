using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ErrorOr;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Seeding;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.DemoData;
using FixFlow.Api.Features.DemoData.ResetDemoData;
using FixFlow.Api.IntegrationTests.Auth;
using FixFlow.Api.IntegrationTests.Clients;
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
        (await verificationDbContext.WorkOrders.CountAsync(cancellationToken)).ShouldBe(23);
        (await verificationDbContext.Parts.CountAsync(cancellationToken)).ShouldBe(11);
        (await verificationDbContext.Users.AnyAsync(user => user.Id == accountCreatedByAdmin.Id, cancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Restore_Demo_Account_Access_And_Names_When_Demo_Data_Is_Reset()
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
            technician.ChangeFullName("Renamed Technician");
            (await userManager.UpdateAsync(technician)).Succeeded.ShouldBeTrue();
            var additionalTechnician = (await userManager.FindByEmailAsync(DemoDataSeeder.AnnaKowalczykEmail)).ShouldNotBeNull();
            additionalTechnician.ChangeFullName("Renamed Additional Technician");
            (await userManager.UpdateAsync(additionalTechnician)).Succeeded.ShouldBeTrue();
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
        restoredTechnician.FullName.ShouldBe(DemoUsersOptions.TechnicianFullName);
        (await verificationUserManager.FindByEmailAsync(DemoDataSeeder.AnnaKowalczykEmail)).ShouldNotBeNull().FullName.ShouldBe("Anna Kowalczyk");
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await verificationDbContext.RefreshTokens.AnyAsync(token => token.UserId == restoredTechnician.Id && token.RevokedAt == null, cancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Deactivate_Former_Technician_Again_When_Demo_Data_Is_Reset_After_Activation()
    {
        await using var demoFactory = CreateFactoryWithDemoData();
        await using (var scope = demoFactory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var formerTechnician = (await userManager.FindByEmailAsync(DemoDataSeeder.PiotrZielinskiEmail)).ShouldNotBeNull();
            formerTechnician.Activate();
            (await userManager.UpdateAsync(formerTechnician)).Succeeded.ShouldBeTrue();
        }

        var result = await ResetDemoDataAsync(demoFactory);

        result.IsError.ShouldBeFalse();
        await using var verificationScope = demoFactory.Services.CreateAsyncScope();
        var verificationUserManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var restoredTechnician = (await verificationUserManager.FindByEmailAsync(DemoDataSeeder.PiotrZielinskiEmail)).ShouldNotBeNull();
        restoredTechnician.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Return_Disabled_Error_When_Demo_Data_Is_Disabled()
    {
        var result = await ResetDemoDataAsync(Factory);

        result.FirstError.ShouldBe(DemoDataErrors.Disabled);
    }

    [Fact]
    public async Task Should_Return_No_Content_When_Admin_Resets_Demo_Data()
    {
        await using var demoFactory = CreateFactoryWithDemoData();
        using var adminClient = await CreateAuthenticatedClientAsync(demoFactory, Roles.Admin);

        using var response = await PostResetAsync(adminClient);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Resetting_Demo_Data_While_Disabled()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);

        using var response = await PostResetAsync(adminClient);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, DemoDataErrors.Disabled.Code);
    }

    [Fact]
    public async Task Should_Invalidate_Cached_Client_List_When_Demo_Data_Is_Reset()
    {
        await using var demoFactory = CreateFactoryWithDemoData();
        using var dispatcherClient = await CreateAuthenticatedClientAsync(demoFactory, Roles.Dispatcher);
        using var adminClient = await CreateAuthenticatedClientAsync(demoFactory, Roles.Admin);
        var clientIdsBeforeReset = await GetClientIdsAsync(dispatcherClient);

        using var response = await PostResetAsync(adminClient);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var clientIdsAfterReset = await GetClientIdsAsync(dispatcherClient);
        clientIdsAfterReset.Count.ShouldBe(clientIdsBeforeReset.Count);
        clientIdsAfterReset.ShouldNotContain(clientId => clientIdsBeforeReset.Contains(clientId));
    }

    private WebApplicationFactory<Program> CreateFactoryWithDemoData() =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting("Seed:DemoData:Enabled", "true"));

    private async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> targetFactory, string role)
    {
        var user = await CreateUserAsync(role);
        var client = targetFactory.CreateClient();
        var tokens = await client.LoginAsync(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private static Task<HttpResponseMessage> PostResetAsync(HttpClient client) =>
        client.PostAsync(new Uri("/api/v1/demo-data/reset", UriKind.Relative), null, TestContext.Current.CancellationToken);

    private static async Task<List<Guid>> GetClientIdsAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<ClientResponse>>(ClientRequests.ClientsUri, TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull().Items.Select(item => item.Id).ToList();
    }

    private static async Task<ErrorOr<Success>> ResetDemoDataAsync(WebApplicationFactory<Program> targetFactory)
    {
        await using var scope = targetFactory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ResetDemoDataHandler>().HandleAsync(TestContext.Current.CancellationToken);
    }
}
