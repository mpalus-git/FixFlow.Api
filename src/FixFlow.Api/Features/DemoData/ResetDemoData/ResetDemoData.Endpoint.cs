using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.DemoData.ResetDemoData;

public static class ResetDemoDataEndpoint
{
    public static RouteGroupBuilder MapResetDemoData(this RouteGroupBuilder group)
    {
        group.MapPost("/reset", async (ResetDemoDataHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                return result.ToNoContentOrProblem();
            })
            .WithName("ResetDemoData")
            .WithSummary("Reset demo data to its initial state")
            .WithDescription("Deletes all clients, devices, parts, work orders and service entries and recreates the demo data set. Restores the demo dispatcher and technician accounts: configured passwords, active status, no sign-in lockout, and revokes their refresh tokens. Accounts created by administrators are kept. Available to administrators on instances with demo data enabled; otherwise returns 409.")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
