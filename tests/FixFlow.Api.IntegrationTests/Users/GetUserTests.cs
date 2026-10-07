using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class GetUserTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_User_Account_When_Dispatcher_Gets_It()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician, "Anna Kowalczyk");

        var user = await dispatcherClient.GetFromJsonAsync<UserResponse>(new Uri($"/api/v1/users/{technician.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        user.ShouldBe(new UserResponse(technician.Id, technician.Email, "Anna Kowalczyk", Roles.Technician, IsActive: true));
    }
}
