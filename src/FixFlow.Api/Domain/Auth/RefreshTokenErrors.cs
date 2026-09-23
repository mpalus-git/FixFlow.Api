using ErrorOr;

namespace FixFlow.Api.Domain.Auth;

public static class RefreshTokenErrors
{
    public static readonly Error Invalid = Error.Unauthorized("RefreshToken.Invalid", "Refresh token is invalid.");

    public static readonly Error Expired = Error.Unauthorized("RefreshToken.Expired", "Refresh token has expired.");

    public static readonly Error Reused = Error.Unauthorized("RefreshToken.Reused", "Refresh token was already used. All sessions of this token family were revoked.");
}
