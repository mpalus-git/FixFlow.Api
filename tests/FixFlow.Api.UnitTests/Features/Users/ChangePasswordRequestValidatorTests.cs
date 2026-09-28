using FixFlow.Api.Features.Users.ChangePassword;

namespace FixFlow.Api.UnitTests.Features.Users;

public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_New_Password_Differs_From_Current_One()
    {
        var result = _validator.Validate(new ChangePasswordRequest("OldSecret1!", "NewSecret1!"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_New_Password_Equals_Current_One()
    {
        var result = _validator.Validate(new ChangePasswordRequest("Secret1!", "Secret1!"));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    public void Should_Reject_Request_When_New_Password_Is_Empty_Or_Too_Short(string newPassword)
    {
        var result = _validator.Validate(new ChangePasswordRequest("OldSecret1!", newPassword));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Should_Reject_Request_When_Current_Password_Is_Empty()
    {
        var result = _validator.Validate(new ChangePasswordRequest(string.Empty, "NewSecret1!"));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ChangePasswordRequest.CurrentPassword));
    }
}
