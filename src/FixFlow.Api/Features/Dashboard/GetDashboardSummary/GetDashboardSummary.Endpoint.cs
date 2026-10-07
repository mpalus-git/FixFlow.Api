using FixFlow.Api.Common.Auth;

namespace FixFlow.Api.Features.Dashboard.GetDashboardSummary;

public static class GetDashboardSummaryEndpoint
{
    public static RouteGroupBuilder MapGetDashboardSummary(this RouteGroupBuilder group)
    {
        group.MapGet("/summary", async (GetDashboardSummaryHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("GetDashboardSummary")
            .WithSummary("Get dashboard summary")
            .WithDescription("Returns aggregated work order counts read from a single consistent database snapshot: the number of work orders in each status in lifecycle order (including zeros), the number of work orders flagged as overdue (the same criterion as the isOverdue=true filter of the work order list), the number of active parts out of stock (the same criterion as the inStock=false filter of the part list), and the workload of every active technician sorted by full name and email, including technicians without work orders. The current week runs from Monday to Sunday in the Europe/Warsaw time zone; its bounds are returned as calendar dates usable as dueFrom and dueTo of the work order list. Available to dispatchers and administrators.")
            .RequireAuthorization(AuthorizationPolicies.DispatcherOrAdmin)
            .Produces<DashboardSummaryResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}
