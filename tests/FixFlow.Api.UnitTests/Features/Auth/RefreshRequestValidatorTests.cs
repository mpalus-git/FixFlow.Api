using FixFlow.Api.Features.Auth.Refresh;

namespace FixFlow.Api.UnitTests.Features.Auth;

public sealed class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Refresh_Token_Is_Provided()
    {
        var result = _validator.Validate(new RefreshRequest("opaque-refresh-token"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Refresh_Token_Is_Empty()
    {
        var result = _validator.Validate(new RefreshRequest(string.Empty));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(RefreshRequest.RefreshToken));
    }
}
