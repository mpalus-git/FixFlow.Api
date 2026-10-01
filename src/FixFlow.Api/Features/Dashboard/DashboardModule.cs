using FixFlow.Api.Features.Dashboard.GetDashboardSummary;

namespace FixFlow.Api.Features.Dashboard;

public static class DashboardModule
{
    public static IServiceCollection AddDashboardFeatures(this IServiceCollection services)
    {
        services.AddScoped<GetDashboardSummaryHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Dashboard")
            .MapGroup("/api/v{version:apiVersion}/dashboard")
            .HasApiVersion(1)
            .WithTags("Dashboard");

        group.MapGetDashboardSummary();

        return app;
    }
}
