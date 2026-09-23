using System.Net;

namespace FixFlow.Api.IntegrationTests.Health;

public sealed class LivenessTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Healthy_When_Liveness_Endpoint_Is_Called()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("Healthy");
    }
}
