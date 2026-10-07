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
    public async Task Should_Cache_Preflight_For_Two_Hours_When_Origin_Is_Configured()
    {
        using var client = Factory.CreateClient();
        using var request = CreatePreflightRequest(FixFlowApiFactory.AllowedClientOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.GetValues("Access-Control-Max-Age").ShouldBe(["7200"]);
    }

    [Fact]
    public async Task Should_Not_Allow_Preflight_Request_When_Origin_Is_Not_Configured()
    {
        using var client = Factory.CreateClient();
        using var request = CreatePreflightRequest("https://unknown.example.com");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Expose_ETag_Retry_After_And_Content_Disposition_Headers_When_Origin_Is_Configured()
    {
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.Add("Origin", FixFlowApiFactory.AllowedClientOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.GetValues("Access-Control-Expose-Headers")
            .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries))
            .ShouldBe(["Retry-After", "ETag", "Content-Disposition"], ignoreOrder: true);
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
