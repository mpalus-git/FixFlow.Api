using ErrorOr;

namespace FixFlow.Api.Features.Users;

public static class UserErrors
{
    public static readonly Error DuplicateEmail = Error.Conflict("User.DuplicateEmail", "A user with this email address already exists.");
}
