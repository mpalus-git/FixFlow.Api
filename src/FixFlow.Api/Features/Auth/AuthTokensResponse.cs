using System.ComponentModel;

namespace FixFlow.Api.Features.Auth;

[Description("Access and refresh tokens issued for an authenticated session.")]
public sealed record AuthTokensResponse(
    [property: Description("JWT access token to send in the Authorization header as a Bearer token.")] string AccessToken,
    [property: Description("UTC time when the access token expires.")] DateTimeOffset AccessTokenExpiresAt,
    [property: Description("Opaque refresh token used once to obtain a new token pair.")] string RefreshToken,
    [property: Description("UTC time when the refresh token expires.")] DateTimeOffset RefreshTokenExpiresAt);
