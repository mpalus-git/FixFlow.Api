using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Features.Auth.Login;
using Microsoft.AspNetCore.Hosting;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class AuthRateLimitTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Too_Many_Requests_Problem_When_Login_Limit_Is_Exceeded()
    {
        await using var limitedFactory = Factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "2"));
        using var client = limitedFactory.CreateClient();
        var request = new LoginRequest("nobody@fixflow.test", "Some1!password");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowedResponse = await client.PostAsJsonAsync(AuthRequests.LoginUri, request, TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var response = await client.PostAsJsonAsync(AuthRequests.LoginUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.Contains("Retry-After").ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Share_Limit_Between_Login_And_Refresh_When_Requests_Come_From_Same_Client()
    {
        await using var limitedFactory = Factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "1"));
        using var client = limitedFactory.CreateClient();
        using var loginResponse = await client.PostAsJsonAsync(AuthRequests.LoginUri, new LoginRequest("nobody@fixflow.test", "Some1!password"), TestContext.Current.CancellationToken);

        using var refreshResponse = await client.PostRefreshAsync("some-refresh-token");

        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
