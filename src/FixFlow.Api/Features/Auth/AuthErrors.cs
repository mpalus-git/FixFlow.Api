using ErrorOr;

namespace FixFlow.Api.Features.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
}
