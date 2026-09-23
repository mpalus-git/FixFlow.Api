using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Behaviors;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Auth.Login;

public static class LoginEndpoint
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginRequest request, LoginHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.Match<IResult>(tokens => TypedResults.Ok(tokens), errors => errors.ToProblem());
            })
            .WithName("Login")
            .WithSummary("Log in with email and password")
            .WithDescription("Returns a short-lived JWT access token and a refresh token that starts a new session. Wrong credentials and locked out accounts return the same 401 response, so the endpoint does not reveal whether an account exists. Requests are rate limited per client IP address.")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimiting.PolicyName)
            .WithRequestValidation<LoginRequest>()
            .Produces<AuthTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return group;
    }
}
