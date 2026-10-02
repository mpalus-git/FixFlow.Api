using System.Net;

namespace FixFlow.Api.IntegrationTests.Health;

public sealed class ReadinessTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri SystemReadinessUri = new("/api/v1/system/ready", UriKind.Relative);

    [Fact]
    public async Task Should_Return_Healthy_When_Database_Is_Available()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Should_Return_Healthy_When_System_Readiness_Is_Checked_And_Database_Is_Available()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(SystemReadinessUri, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Should_Return_Cors_Headers_When_System_Readiness_Is_Requested_From_Allowed_Origin()
    {
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, SystemReadinessUri);
        request.Headers.Add("Origin", FixFlowApiFactory.AllowedClientOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([FixFlowApiFactory.AllowedClientOrigin]);
    }
}
