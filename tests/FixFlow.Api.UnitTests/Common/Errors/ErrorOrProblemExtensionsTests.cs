using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Errors;
using FixFlow.Api.Domain.Photos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixFlow.Api.UnitTests.Common.Errors;

public sealed class ErrorOrProblemExtensionsTests
{
    [Fact]
    public void Should_Return_Validation_Problem_With_Camel_Case_Keys_Grouped_By_Code_When_All_Errors_Are_Validation()
    {
        Error[] errors =
        [
            Error.Validation("Email", "Email is required."),
            Error.Validation("Email", "Email is invalid."),
            Error.Validation("Password", "Password is required."),
        ];

        var result = errors.ToProblem();

        var problem = result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors["email"].ShouldBe(["Email is required.", "Email is invalid."]);
        problem.ProblemDetails.Errors["password"].ShouldBe(["Password is required."]);
    }

    [Theory]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    public void Should_Map_Error_Type_To_Status_Code_When_Error_Is_Not_Validation(ErrorType errorType, int expectedStatusCode)
    {
        Error[] errors = [Error.Custom((int)errorType, "Sample.Code", "Sample description.")];

        var result = errors.ToProblem();

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(expectedStatusCode);
        problem.ProblemDetails.Detail.ShouldBe("Sample description.");
        problem.ProblemDetails.Extensions[ErrorOrProblemExtensions.ErrorCodeExtension].ShouldBe("Sample.Code");
    }

    [Fact]
    public void Should_Map_Precondition_Errors_To_412_And_428_When_Errors_Are_Precondition_Errors()
    {
        var failed = new[] { PreconditionErrors.Failed }.ToProblem();
        var required = new[] { PreconditionErrors.Required }.ToProblem();

        failed.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status412PreconditionFailed);
        required.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status428PreconditionRequired);
    }

    [Fact]
    public void Should_Map_Photo_Content_Errors_To_413_And_415_When_Errors_Are_Photo_Content_Errors()
    {
        var tooLarge = new[] { PhotoErrors.ContentTooLarge }.ToProblem();
        var unsupportedMediaType = new[] { PhotoErrors.UnsupportedMediaType }.ToProblem();

        tooLarge.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status413PayloadTooLarge);
        unsupportedMediaType.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public void Should_Use_First_Error_When_Errors_Have_Mixed_Types()
    {
        Error[] errors =
        [
            Error.Conflict("WorkOrder.Conflict", "Conflict description."),
            Error.Validation("Email", "Email is required."),
        ];

        var result = errors.ToProblem();

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
