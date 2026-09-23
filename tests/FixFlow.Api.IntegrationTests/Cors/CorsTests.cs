using System.Net;
using FixFlow.Api.IntegrationTests.Auth;

namespace FixFlow.Api.IntegrationTests.Cors;

public sealed class CorsTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Allow_Preflight_Request_When_Origin_Is_Configured()
    {
        using var client = Factory.CreateClient();
        using var request = CreatePreflightRequest(FixFlowApiFactory.AllowedClientOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([FixFlowApiFactory.AllowedClientOrigin]);
    }

    [Fact]
    public async Task Should_Not_Allow_Preflight_Request_When_Origin_Is_Not_Configured()
    {
        using var client = Factory.CreateClient();
        using var request = CreatePreflightRequest("https://unknown.example.com");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, AuthRequests.LoginUri);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }
}
