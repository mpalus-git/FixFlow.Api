using ErrorOr;
using FixFlow.Api.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixFlow.Api.UnitTests.Common.Errors;

public sealed class ErrorOrResultExtensionsTests
{
    private static readonly Error NotFound = Error.NotFound("Sample.NotFound", "Sample not found.");

    [Fact]
    public void Should_Return_Ok_With_Value_When_Result_Is_Success()
    {
        ErrorOr<string> result = "value";

        var httpResult = result.ToOkOrProblem();

        httpResult.ShouldBeOfType<Ok<string>>().Value.ShouldBe("value");
    }

    [Fact]
    public void Should_Return_No_Content_When_Result_Is_Success()
    {
        ErrorOr<Success> result = Result.Success;

        var httpResult = result.ToNoContentOrProblem();

        httpResult.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void Should_Return_Created_With_Location_When_Result_Is_Success()
    {
        ErrorOr<string> result = "value";

        var httpResult = result.ToCreatedOrProblem(value => $"/api/v1/samples/{value}");

        var created = httpResult.ShouldBeOfType<Created<string>>();
        created.Value.ShouldBe("value");
        created.Location.ShouldBe("/api/v1/samples/value");
    }

    [Fact]
    public void Should_Return_Created_Without_Location_When_Location_Is_Not_Given()
    {
        ErrorOr<string> result = "value";

        var httpResult = result.ToCreatedOrProblem();

        httpResult.ShouldBeOfType<Created<string>>().Location.ShouldBeNull();
    }

    [Fact]
    public void Should_Return_Problem_When_Result_Is_Error()
    {
        ErrorOr<string> result = NotFound;

        IResult[] httpResults = [result.ToOkOrProblem(), result.ToNoContentOrProblem(), result.ToCreatedOrProblem()];

        httpResults.ShouldAllBe(httpResult => ((ProblemHttpResult)httpResult).StatusCode == StatusCodes.Status404NotFound);
    }
}
