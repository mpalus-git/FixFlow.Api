using Microsoft.IdentityModel.JsonWebTokens;

namespace FixFlow.Api.Common.Auth;

public static class AuthClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Role = "role";
}
