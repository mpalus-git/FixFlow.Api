using System.Net;
using System.Text;
using FixFlow.Api.IntegrationTests.Auth;

namespace FixFlow.Api.IntegrationTests.Errors;

public sealed class MalformedRequestBodyTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData("")]
    [InlineData("{\"email\":")]
    public async Task Should_Return_BadRequest_Problem_When_Request_Body_Is_Not_Valid_Json(string body)
    {
        using var client = Factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(AuthRequests.LoginUri, content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
