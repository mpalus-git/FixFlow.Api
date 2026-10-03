using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users.CreateUser;

namespace FixFlow.Api.UnitTests.Features.Users;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Dispatcher)]
    [InlineData(Roles.Technician)]
    public void Should_Accept_Request_When_Role_Is_Known(string role)
    {
        var result = _validator.Validate(new CreateUserRequest("new.user@fixflow.test", "Jan Kowalski", "Secret1!", role));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Manager")]
    [InlineData("admin")]
    public void Should_Reject_Request_When_Role_Is_Empty_Or_Unknown(string role)
    {
        var result = _validator.Validate(new CreateUserRequest("new.user@fixflow.test", "Jan Kowalski", "Secret1!", role));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateUserRequest.Role));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Should_Reject_Request_When_Email_Is_Empty_Or_Invalid(string email)
    {
        var result = _validator.Validate(new CreateUserRequest(email, "Jan Kowalski", "Secret1!", Roles.Technician));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    public void Should_Reject_Request_When_Password_Is_Empty_Or_Shorter_Than_Eight_Characters(string password)
    {
        var result = _validator.Validate(new CreateUserRequest("new.user@fixflow.test", "Jan Kowalski", password, Roles.Technician));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Request_When_Full_Name_Is_Empty(string fullName)
    {
        var result = _validator.Validate(new CreateUserRequest("new.user@fixflow.test", fullName, "Secret1!", Roles.Technician));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateUserRequest.FullName));
    }

    [Fact]
    public void Should_Reject_Request_When_Full_Name_Is_Longer_Than_100_Characters()
    {
        var result = _validator.Validate(new CreateUserRequest("new.user@fixflow.test", new string('a', 101), "Secret1!", Roles.Technician));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateUserRequest.FullName));
    }
}
