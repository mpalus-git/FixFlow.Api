using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FluentValidation;

namespace FixFlow.Api.Features.ServiceEntries;

public static class ServiceEntriesModule
{
    public static IServiceCollection AddServiceEntriesFeatures(this IServiceCollection services)
    {
        services.AddScoped<AddServiceEntryHandler>();
        services.AddSingleton<IValidator<AddServiceEntryRequest>, AddServiceEntryRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapServiceEntriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("ServiceEntries")
            .MapGroup("/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/service-entries")
            .HasApiVersion(1)
            .WithTags("ServiceEntries")
            .RequireAuthorization();

        group.MapAddServiceEntry();

        return app;
    }
}
