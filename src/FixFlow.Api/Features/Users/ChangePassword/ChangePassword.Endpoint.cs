using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;
using FixFlow.Api.Features.Auth;

namespace FixFlow.Api.Features.Users.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static RouteGroupBuilder MapChangePassword(this RouteGroupBuilder group)
    {
        group.MapPost("/me/password", async (ChangePasswordRequest request, ClaimsPrincipal user, ChangePasswordHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(user, request, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithName("ChangePassword")
            .WithSummary("Change the password of the current user")
            .WithDescription("Replaces the password of the signed-in user after checking the current one. All refresh tokens of the user are revoked, which signs out other devices, and a new token pair is returned for the calling device. Access tokens issued before stay valid until they expire. Requests are rate limited per client IP address.")
            .RequireAuthorization()
            .RequireRateLimiting(AuthRateLimiting.PolicyName)
            .WithRequestValidation<ChangePasswordRequest>()
            .Produces<AuthTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }
}
