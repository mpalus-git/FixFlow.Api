using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class ListUsersTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Only_Technicians_Ordered_By_Email_When_Dispatcher_Filters_By_Technician_Role()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var firstTechnician = await CreateUserAsync(Roles.Technician);
        var secondTechnician = await CreateUserAsync(Roles.Technician);
        await CreateUserAsync(Roles.Admin);

        var page = await GetPageAsync(client, $"?role={Roles.Technician}");

        page.Items.Select(user => user.Email).ShouldBe(new[] { firstTechnician.Email, secondTechnician.Email }.Order(StringComparer.Ordinal));
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
    public async Task Should_Return_Validation_Problem_When_Role_Is_Unknown()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Dispatcher);

        using var response = await client.GetAsync(new Uri("/api/v1/users?role=Manager", UriKind.Relative), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("Role");
    }

    [Fact]
    public async Task Should_Return_Forbidden_When_Technician_Lists_Users()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.GetAsync(new Uri("/api/v1/users", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<PagedResponse<UserResponse>> GetPageAsync(HttpClient client, string query)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<UserResponse>>(new Uri($"/api/v1/users{query}", UriKind.Relative), TestContext.Current.CancellationToken);
        return page.ShouldNotBeNull();
    }
}
