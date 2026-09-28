using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Users.ResetPassword;
using FixFlow.Api.IntegrationTests.Auth;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class ResetPasswordTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private const string NewPassword = "NewSecret1!";

    [Fact]
    public async Task Should_Accept_Only_New_Password_And_Revoke_Sessions_When_Admin_Resets_Password()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var technician = await CreateUserAsync(Roles.Technician);
        var technicianTokens = await adminClient.LoginAsync(technician);

        using var response = await adminClient.PostAsJsonAsync(ResetPasswordUri(technician.Id), new ResetPasswordRequest(NewPassword), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var oldPasswordLogin = await PostLoginAsync(adminClient, technician.Email, technician.Password);
        using var newPasswordLogin = await PostLoginAsync(adminClient, technician.Email, NewPassword);
        using var refreshResponse = await adminClient.PostRefreshAsync(technicianTokens.RefreshToken);
        oldPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Keep_Current_Password_When_New_Password_Does_Not_Meet_Password_Policy()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);

        using var response = await adminClient.PostAsJsonAsync(ResetPasswordUri(dispatcher.Id), new ResetPasswordRequest("onlyletters"), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("newPassword");
        using var currentPasswordLogin = await PostLoginAsync(adminClient, dispatcher.Email, dispatcher.Password);
        currentPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Uri ResetPasswordUri(Guid userId) => new($"/api/v1/users/{userId}/password", UriKind.Relative);

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(AuthRequests.LoginUri, new LoginRequest(email, password), TestContext.Current.CancellationToken);
}
