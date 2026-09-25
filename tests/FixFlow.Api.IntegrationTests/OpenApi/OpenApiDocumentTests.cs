using System.Net;
using System.Text.Json;

namespace FixFlow.Api.IntegrationTests.OpenApi;

public sealed class OpenApiDocumentTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Expose_OpenApi_Document_When_V1_Is_Requested()
    {
        using var document = await GetDocumentAsync();

        document.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("FixFlow API");
    }

    [Fact]
    public async Task Should_Describe_Numbers_Without_Text_Alternative()
    {
        using var document = await GetDocumentAsync();

        var pageSchema = document.RootElement
            .GetProperty("paths").GetProperty("/api/v1/clients").GetProperty("get")
            .GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "page")
            .GetProperty("schema");
        pageSchema.GetProperty("type").GetString().ShouldBe("integer");
        pageSchema.TryGetProperty("pattern", out _).ShouldBeFalse();
    }

    private async Task<JsonDocument> GetDocumentAsync()
    {
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
    }
}
