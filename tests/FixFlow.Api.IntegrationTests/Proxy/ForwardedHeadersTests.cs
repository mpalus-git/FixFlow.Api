using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.IntegrationTests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FixFlow.Api.IntegrationTests.Proxy;

public sealed class ForwardedHeadersTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private const string FirstClientAddress = "203.0.113.10";
    private const string SecondClientAddress = "203.0.113.20";

    [Fact]
    public async Task Should_Limit_Auth_Requests_Per_Forwarded_Client_When_Forwarded_Headers_Are_Enabled()
    {
        await using var proxiedFactory = CreateFactory(forwardedHeadersEnabled: true);
        using var client = proxiedFactory.CreateClient();

        using var firstResponse = await LoginAsync(client, FirstClientAddress);
        using var limitedResponse = await LoginAsync(client, FirstClientAddress);
        using var otherClientResponse = await LoginAsync(client, SecondClientAddress);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherClientResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Ignore_Forwarded_Client_Address_When_Forwarded_Headers_Are_Disabled()
    {
        await using var directFactory = CreateFactory(forwardedHeadersEnabled: false);
        using var client = directFactory.CreateClient();

        using var firstResponse = await LoginAsync(client, FirstClientAddress);
        using var otherClientResponse = await LoginAsync(client, SecondClientAddress);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        otherClientResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Should_Describe_Https_Server_In_OpenApi_Document_When_Proxy_Forwards_Https()
    {
        await using var proxiedFactory = CreateFactory(forwardedHeadersEnabled: true);
        using var client = proxiedFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/openapi/v1.json", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        var serverUrl = document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString();
        serverUrl.ShouldNotBeNull().ShouldStartWith("https://");
    }

    private WebApplicationFactory<Program> CreateFactory(bool forwardedHeadersEnabled) =>
        Factory.WithWebHostBuilder(builder => builder
            .UseSetting("FORWARDEDHEADERS_ENABLED", forwardedHeadersEnabled ? "true" : "false")
            .UseSetting("RateLimiting:Auth:PermitLimit", "1"));

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string forwardedClientAddress)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, AuthRequests.LoginUri)
        {
            Content = JsonContent.Create(new LoginRequest("nobody@fixflow.test", "Some1!password")),
        };
        request.Headers.Add("X-Forwarded-For", forwardedClientAddress);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
