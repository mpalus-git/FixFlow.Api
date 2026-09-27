using System.Net;
using System.Text.Json;

namespace FixFlow.Api.IntegrationTests.OpenApi;

public sealed class OpenApiDocumentTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Expose_OpenApi_Document_When_V1_Is_Requested()
    {
        using var document = await GetDocumentAsync();

        var info = document.RootElement.GetProperty("info");
        info.GetProperty("title").GetString().ShouldBe("FixFlow API");
    }

    [Fact]
    public async Task Should_Describe_Error_Code_In_Problem_Details()
    {
        using var document = await GetDocumentAsync();

        var problemDetails = Schema(document, "ProblemDetails");
        problemDetails.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
        problemDetails.GetProperty("properties").GetProperty("errorCode").GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
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

    [Theory]
    [InlineData("WorkOrderPriority")]
    [InlineData("WorkOrderStatus")]
    public async Task Should_Describe_Enum_As_String_When_Schema_Is_Generated(string schemaName)
    {
        using var document = await GetDocumentAsync();

        var schema = Schema(document, schemaName);
        schema.GetProperty("type").GetString().ShouldBe("string");
        schema.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Describe_Amount_As_Decimal_Number()
    {
        using var document = await GetDocumentAsync();

        var unitPrice = Schema(document, "PartResponse").GetProperty("properties").GetProperty("unitPrice");
        unitPrice.GetProperty("type").GetString().ShouldBe("number");
        unitPrice.GetProperty("format").GetString().ShouldBe("decimal");
    }

    private static JsonElement Schema(JsonDocument document, string schemaName) =>
        document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(schemaName);

    private async Task<JsonDocument> GetDocumentAsync()
    {
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
    }
}
