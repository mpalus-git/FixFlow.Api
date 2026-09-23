using System.ComponentModel;

namespace FixFlow.Api.Features.Auth.Login;

[Description("Credentials of an existing user account.")]
public sealed record LoginRequest(
    [property: Description("Email address of the user account.")] string Email,
    [property: Description("Password of the user account.")] string Password);
