using ErrorOr;

namespace FixFlow.Api.Features.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user account was not found.");

    public static readonly Error DuplicateEmail = Error.Conflict("User.DuplicateEmail", "A user with this email address already exists.");

    public static readonly Error CannotDeactivateSelf = Error.Conflict("User.CannotDeactivateSelf", "Administrators cannot deactivate their own account.");

    public static readonly Error HasOpenWorkOrders = Error.Conflict(
        "User.HasOpenWorkOrders",
        "The user has assigned or in-progress work orders. Unassign or complete them before deactivating the account.");
}
