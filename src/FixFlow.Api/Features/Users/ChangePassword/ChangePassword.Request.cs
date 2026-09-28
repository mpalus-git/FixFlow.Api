using System.ComponentModel;

namespace FixFlow.Api.Features.Users.ChangePassword;

[Description("Change of the password of the signed-in user.")]
public sealed record ChangePasswordRequest(
    [property: Description("Current password of the account.")] string CurrentPassword,
    [property: Description("New password. At least 8 characters with an uppercase letter, a lowercase letter, a digit and a non-alphanumeric character, different from the current one.")] string NewPassword);
