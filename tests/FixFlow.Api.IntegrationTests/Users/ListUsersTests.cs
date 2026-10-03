using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class ListUsersTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Only_Technicians_Ordered_By_Full_Name_When_Dispatcher_Filters_By_Technician_Role()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technicianNamedZofia = await CreateUserAsync(Roles.Technician, "Zofia Adamska");
        var technicianNamedAdam = await CreateUserAsync(Roles.Technician, "Adam Zawadzki");
        await CreateUserAsync(Roles.Admin);

        var page = await GetPageAsync(client, $"?role={Roles.Technician}");

        page.Items.Select(user => user.Id).ShouldBe([technicianNamedAdam.Id, technicianNamedZofia.Id]);
        page.Items.Select(user => user.FullName).ShouldBe(["Adam Zawadzki", "Zofia Adamska"]);
        page.Items.ShouldAllBe(user => user.Role == Roles.Technician);
        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Return_Users_Of_All_Roles_When_Admin_Lists_Without_Role_Filter()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Admin);
        await CreateUserAsync(Roles.Dispatcher);
        await CreateUserAsync(Roles.Technician);

        var page = await GetPageAsync(client, string.Empty);

        page.Items.Select(user => user.Role).Order().ShouldBe([Roles.Admin, Roles.Dispatcher, Roles.Technician]);
    }

    [Fact]
    public async Task Should_Return_Only_Active_Technicians_When_Dispatcher_Filters_By_Active_State()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var activeTechnician = await CreateUserAsync(Roles.Technician);
        var deactivatedTechnician = await CreateUserAsync(Roles.Technician);
        await DeactivateUserDirectlyAsync(deactivatedTechnician.Id);

        var activePage = await GetPageAsync(client, $"?role={Roles.Technician}&isActive=true");
        var deactivatedPage = await GetPageAsync(client, $"?role={Roles.Technician}&isActive=false");

        activePage.Items.ShouldHaveSingleItem().ShouldBe(new UserResponse(activeTechnician.Id, activeTechnician.Email, activeTechnician.FullName, Roles.Technician, IsActive: true));
        deactivatedPage.Items.ShouldHaveSingleItem().ShouldBe(new UserResponse(deactivatedTechnician.Id, deactivatedTechnician.Email, deactivatedTechnician.FullName, Roles.Technician, IsActive: false));
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Role_Is_Unknown()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri("/api/v1/users?role=Manager", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("role");
    }

    private static async Task<PagedResponse<UserResponse>> GetPageAsync(HttpClient client, string query)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<UserResponse>>(new Uri($"/api/v1/users{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }
}
