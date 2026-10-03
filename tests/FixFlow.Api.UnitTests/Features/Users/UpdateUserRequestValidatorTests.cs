using FixFlow.Api.Features.Users.UpdateUser;

namespace FixFlow.Api.UnitTests.Features.Users;

public sealed class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Full_Name_Is_Given()
    {
        _validator.Validate(new UpdateUserRequest("Jan Kowalski")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Request_When_Full_Name_Is_Empty(string fullName)
    {
        var result = _validator.Validate(new UpdateUserRequest(fullName));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateUserRequest.FullName));
    }

    [Fact]
    public void Should_Reject_Request_When_Full_Name_Is_Longer_Than_100_Characters()
    {
        var result = _validator.Validate(new UpdateUserRequest(new string('a', 101)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateUserRequest.FullName));
    }
}
