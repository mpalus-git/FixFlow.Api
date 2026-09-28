using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.UnitTests.Common.Concurrency;

public sealed class ConditionalRequestExtensionsTests
{
    [Fact]
    public async Task Should_Set_ETag_Header_When_Result_Is_Executed()
    {
        var httpContext = CreateHttpContext();

        await TypedResults.NoContent().WithETag(17).ExecuteAsync(httpContext);

        httpContext.Response.Headers.ETag.ToString().ShouldBe("\"17\"");
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Should_Return_Ok_With_ETag_When_Versioned_Result_Is_Success()
    {
        ErrorOr<Versioned<string>> result = new Versioned<string>("value", 5);
        var httpContext = CreateHttpContext();

        await result.ToOkWithETagOrProblem().ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        httpContext.Response.Headers.ETag.ToString().ShouldBe("\"5\"");
    }

    [Fact]
    public void Should_Return_Precondition_Failed_Problem_When_Versioned_Result_Is_Precondition_Error()
    {
        ErrorOr<Versioned<string>> result = PreconditionErrors.Failed;

        var httpResult = result.ToOkWithETagOrProblem();

        httpResult.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status412PreconditionFailed);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        Response = { Body = new MemoryStream() },
    };
}
