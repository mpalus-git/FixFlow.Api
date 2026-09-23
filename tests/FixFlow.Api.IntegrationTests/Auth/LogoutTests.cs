using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth.Logout;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class LogoutTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri LogoutUri = new("/api/v1/auth/logout", UriKind.Relative);

    [Fact]
    public async Task Should_Revoke_Session_When_Logged_In_User_Logs_Out()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var tokens = await client.LoginAsync(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using var logoutResponse = await client.PostAsJsonAsync(LogoutUri, new LogoutRequest(tokens.RefreshToken), TestContext.Current.CancellationToken);
        using var refreshResponse = await client.PostRefreshAsync(tokens.RefreshToken);

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await refreshResponse.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Revoked.Code);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_Problem_When_Logout_Is_Called_Without_Access_Token()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LogoutUri, new LogoutRequest("some-refresh-token"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Should_Keep_Other_User_Session_When_Logout_Uses_Foreign_Refresh_Token()
    {
        var firstUser = await CreateUserAsync(Roles.Technician);
        var secondUser = await CreateUserAsync(Roles.Dispatcher);
        using var client = Factory.CreateClient();
        var firstUserTokens = await client.LoginAsync(firstUser);
        var secondUserTokens = await client.LoginAsync(secondUser);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstUserTokens.AccessToken);

        using var logoutResponse = await client.PostAsJsonAsync(LogoutUri, new LogoutRequest(secondUserTokens.RefreshToken), TestContext.Current.CancellationToken);
        using var refreshResponse = await client.PostRefreshAsync(secondUserTokens.RefreshToken);

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
