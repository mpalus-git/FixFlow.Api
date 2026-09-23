using System.ComponentModel;

namespace FixFlow.Api.Features.Auth.Logout;

[Description("Refresh token of the session to end.")]
public sealed record LogoutRequest(
    [property: Description("Refresh token of the current session.")] string RefreshToken);
