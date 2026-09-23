using System.Net.Http.Json;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Auth.Refresh;

namespace FixFlow.Api.IntegrationTests.Auth;

public static class AuthRequests
{
    public static readonly Uri LoginUri = new("/api/v1/auth/login", UriKind.Relative);
    public static readonly Uri RefreshUri = new("/api/v1/auth/refresh", UriKind.Relative);

    public static async Task<AuthTokensResponse> LoginAsync(this HttpClient client, TestUser user)
    {
        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, user.Password), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        return tokens.ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> PostRefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(RefreshUri, new RefreshRequest(refreshToken), TestContext.Current.CancellationToken);
}
