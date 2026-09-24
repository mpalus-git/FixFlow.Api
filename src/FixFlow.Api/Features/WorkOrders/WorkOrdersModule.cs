using FixFlow.Api.Features.WorkOrders.AssignTechnician;
using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;
using FixFlow.Api.Features.WorkOrders.GetWorkOrder;
using FixFlow.Api.Features.WorkOrders.ListWorkOrders;
using FixFlow.Api.Features.WorkOrders.StartWork;
using FixFlow.Api.Features.WorkOrders.UnassignTechnician;
using FluentValidation;

namespace FixFlow.Api.Features.WorkOrders;

public static class WorkOrdersModule
{
    public static IServiceCollection AddWorkOrdersFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreateWorkOrderHandler>();
        services.AddSingleton<IValidator<CreateWorkOrderRequest>, CreateWorkOrderRequestValidator>();
        services.AddScoped<GetWorkOrderHandler>();
        services.AddScoped<ListWorkOrdersHandler>();
        services.AddSingleton<IValidator<ListWorkOrdersRequest>, ListWorkOrdersRequestValidator>();
        services.AddScoped<AssignTechnicianHandler>();
        services.AddSingleton<IValidator<AssignTechnicianRequest>, AssignTechnicianRequestValidator>();
        services.AddScoped<UnassignTechnicianHandler>();
        services.AddScoped<StartWorkHandler>();

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
        group.MapListWorkOrders();
        group.MapAssignTechnician();
        group.MapUnassignTechnician();
        group.MapStartWork();

        return app;
    }
}
