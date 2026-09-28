using FixFlow.Api.Features.Users.ResetPassword;

namespace FixFlow.Api.UnitTests.Features.Users;

public sealed class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_New_Password_Has_Allowed_Length()
    {
        var result = _validator.Validate(new ResetPasswordRequest("NewSecret1!"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    public void Should_Reject_Request_When_New_Password_Is_Empty_Or_Too_Short(string newPassword)
    {
        var result = _validator.Validate(new ResetPasswordRequest(newPassword));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ResetPasswordRequest.NewPassword));
    }

    [Fact]
    public void Should_Reject_Request_When_New_Password_Is_Longer_Than_128_Characters()
    {
        var result = _validator.Validate(new ResetPasswordRequest(new string('a', 129)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ResetPasswordRequest.NewPassword));
    }
}
