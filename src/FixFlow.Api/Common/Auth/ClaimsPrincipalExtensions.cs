using System.Security.Claims;

namespace FixFlow.Api.Common.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(AuthClaimTypes.UserId)
            ?? throw new InvalidOperationException("Authenticated principal has no user id claim."));
}
