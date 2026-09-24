using System.ComponentModel;

namespace FixFlow.Api.Features.Users;

[Description("User account.")]
public sealed record UserResponse(
    [property: Description("Identifier of the user account.")] Guid Id,
    [property: Description("Email address of the user account.")] string Email,
    [property: Description("Role of the user account.")] string Role);
