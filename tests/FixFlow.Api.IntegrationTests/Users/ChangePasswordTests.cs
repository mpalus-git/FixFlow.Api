using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Users.ChangePassword;
using FixFlow.Api.IntegrationTests.Auth;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class ChangePasswordTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private const string NewPassword = "NewSecret1!";
    private static readonly Uri ChangePasswordUri = new("/api/v1/users/me/password", UriKind.Relative);

    [Fact]
    public async Task Should_Accept_Only_New_Password_And_Sign_Out_Other_Devices_When_Password_Is_Changed()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var otherDeviceTokens = await client.LoginAsync(user);
        var callingDeviceTokens = await client.LoginAsync(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callingDeviceTokens.AccessToken);

        using var response = await client.PostAsJsonAsync(ChangePasswordUri, new ChangePasswordRequest(user.Password, NewPassword), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTokens = (await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        using var oldPasswordLogin = await PostLoginAsync(client, user.Password);
        using var newPasswordLogin = await PostLoginAsync(client, NewPassword);
        using var otherDeviceRefresh = await client.PostRefreshAsync(otherDeviceTokens.RefreshToken);
        using var callingDeviceOldRefresh = await client.PostRefreshAsync(callingDeviceTokens.RefreshToken);
        using var newTokensRefresh = await client.PostRefreshAsync(newTokens.RefreshToken);
        oldPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
        otherDeviceRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        callingDeviceOldRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newTokensRefresh.StatusCode.ShouldBe(HttpStatusCode.OK);

        async Task<HttpResponseMessage> PostLoginAsync(HttpClient httpClient, string password) =>
            await httpClient.PostAsJsonAsync(AuthRequests.LoginUri, new LoginRequest(user.Email, password), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_And_Keep_Password_When_Current_Password_Is_Wrong()
    {
        var user = await CreateUserAsync(Roles.Dispatcher);
        using var client = await CreateAuthenticatedClientAsync(user);

        using var response = await client.PostAsJsonAsync(ChangePasswordUri, new ChangePasswordRequest("WrongSecret1!", NewPassword), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("currentPassword");
        (await client.LoginAsync(user)).AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_New_Password_Does_Not_Meet_Password_Policy()
    {
        var user = await CreateUserAsync(Roles.Dispatcher);
        using var client = await CreateAuthenticatedClientAsync(user);

        using var response = await client.PostAsJsonAsync(ChangePasswordUri, new ChangePasswordRequest(user.Password, "onlyletters"), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("newPassword");
    }
}
