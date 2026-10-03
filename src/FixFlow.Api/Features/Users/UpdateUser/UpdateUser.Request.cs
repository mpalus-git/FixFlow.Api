using System.ComponentModel;

namespace FixFlow.Api.Features.Users.UpdateUser;

[Description("Editable data of a user account.")]
public sealed record UpdateUserRequest(
    [property: Description("First and last name of the user, up to 100 characters.")] string FullName);
