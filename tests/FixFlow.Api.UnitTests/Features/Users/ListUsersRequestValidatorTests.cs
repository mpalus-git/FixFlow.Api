using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users.ListUsers;

namespace FixFlow.Api.UnitTests.Features.Users;

public sealed class ListUsersRequestValidatorTests
{
    private readonly ListUsersRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(Roles.Technician)]
    public void Should_Accept_Request_When_Role_Filter_Is_Omitted_Or_Known(string? role)
    {
        var result = _validator.Validate(new ListUsersRequest(Role: role));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("technician")]
    public void Should_Reject_Request_When_Role_Filter_Is_Unknown(string role)
    {
        var result = _validator.Validate(new ListUsersRequest(Role: role));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ListUsersRequest.Role));
    }
}
