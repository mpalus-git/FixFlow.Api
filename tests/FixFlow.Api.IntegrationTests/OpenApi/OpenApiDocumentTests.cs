using System.Net;
using System.Text.Json;

namespace FixFlow.Api.IntegrationTests.OpenApi;

public sealed class OpenApiDocumentTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Expose_OpenApi_Document_When_V1_Is_Requested()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        document.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("FixFlow API");
    }
}
