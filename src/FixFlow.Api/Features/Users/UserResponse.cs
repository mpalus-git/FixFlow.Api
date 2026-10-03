using System.ComponentModel;

namespace FixFlow.Api.Features.Users;

[Description("User account.")]
public sealed record UserResponse(
    [property: Description("Identifier of the user account.")] Guid Id,
    [property: Description("Email address of the user account.")] string Email,
    [property: Description("First and last name of the user shown in clients instead of the email.")] string FullName,
    [property: Description("Role of the user account.")] string Role,
    [property: Description("False when an administrator deactivated the account; a deactivated account cannot sign in.")] bool IsActive);
