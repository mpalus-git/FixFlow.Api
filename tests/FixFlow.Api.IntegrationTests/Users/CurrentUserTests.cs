using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class CurrentUserTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri CurrentUserUri = new("/api/v1/users/me", UriKind.Relative);

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Technician)]
    public async Task Should_Return_Own_Account_When_Signed_In_User_Requests_Current_User(string role)
    {
        var user = await CreateUserAsync(role);
        await CreateUserAsync(Roles.Dispatcher);
        using var client = await CreateAuthenticatedClientAsync(user);

        var currentUser = await client.GetFromJsonAsync<UserResponse>(CurrentUserUri, TestContext.Current.CancellationToken);

        currentUser.ShouldBe(new UserResponse(user.Id, user.Email, role));
    }
}
