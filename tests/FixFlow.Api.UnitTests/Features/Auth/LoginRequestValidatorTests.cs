using FixFlow.Api.Features.Auth.Login;

namespace FixFlow.Api.UnitTests.Features.Auth;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Email_And_Password_Are_Provided()
    {
        var result = _validator.Validate(new LoginRequest("dispatcher@fixflow.test", "Secret1!"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Should_Reject_Request_When_Email_Is_Empty_Or_Invalid(string email)
    {
        var result = _validator.Validate(new LoginRequest(email, "Secret1!"));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(LoginRequest.Email));
    }

    [Fact]
    public void Should_Reject_Request_When_Password_Is_Empty()
    {
        var result = _validator.Validate(new LoginRequest("dispatcher@fixflow.test", string.Empty));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(LoginRequest.Password));
    }
}
