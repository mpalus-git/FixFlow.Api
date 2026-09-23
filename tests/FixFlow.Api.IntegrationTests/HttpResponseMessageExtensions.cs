using System.Net;
using System.Text.Json;

namespace FixFlow.Api.IntegrationTests;

public static class HttpResponseMessageExtensions
{
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
    }

    public static async Task ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode statusCode, string errorCode)
    {
        response.StatusCode.ShouldBe(statusCode);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = await response.ReadJsonDocumentAsync();
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe(errorCode);
    }

    public static async Task ShouldBeValidationProblemAsync(this HttpResponseMessage response, string propertyName)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = await response.ReadJsonDocumentAsync();
        problem.RootElement.GetProperty("errors").TryGetProperty(propertyName, out _).ShouldBeTrue();
    }
}
