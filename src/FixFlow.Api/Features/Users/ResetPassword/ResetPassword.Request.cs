using System.ComponentModel;

namespace FixFlow.Api.Features.Users.ResetPassword;

[Description("New password set by an administrator for a user account.")]
public sealed record ResetPasswordRequest(
    [property: Description("New password. At least 8 characters with an uppercase letter, a lowercase letter, a digit and a non-alphanumeric character.")] string NewPassword);
