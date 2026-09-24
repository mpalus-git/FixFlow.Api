using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;
using FixFlow.Api.Features.WorkOrders.GetWorkOrder;
using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders;

public static class WorkOrdersModule
{
    public static IServiceCollection AddWorkOrdersFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateWorkOrderHandler>();
        services.AddSingleton<IValidator<CreateWorkOrderRequest>, CreateWorkOrderRequestValidator>();
        services.AddScoped<GetWorkOrderHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapWorkOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("WorkOrders")
            .MapGroup("/api/v{version:apiVersion}/work-orders")
            .HasApiVersion(1)
            .WithTags("WorkOrders")
            .RequireAuthorization();

        group.MapCreateWorkOrder();
        group.MapGetWorkOrder();

        return app;
    }
}
