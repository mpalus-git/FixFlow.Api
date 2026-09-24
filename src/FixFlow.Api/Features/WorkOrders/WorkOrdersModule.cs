using FixFlow.Api.Features.WorkOrders.AssignTechnician;
using FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;
using FixFlow.Api.Features.WorkOrders.CreateWorkOrder;
using FixFlow.Api.Features.WorkOrders.GetWorkOrder;
using FixFlow.Api.Features.WorkOrders.ListWorkOrders;
using FixFlow.Api.Features.WorkOrders.StartWork;
using FixFlow.Api.Features.WorkOrders.UnassignTechnician;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;
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
        services.AddScoped<UpdateWorkOrderHandler>();
        services.AddSingleton<IValidator<UpdateWorkOrderRequest>, UpdateWorkOrderRequestValidator>();
        services.AddScoped<AssignTechnicianHandler>();
        services.AddSingleton<IValidator<AssignTechnicianRequest>, AssignTechnicianRequestValidator>();
        services.AddScoped<UnassignTechnicianHandler>();
        services.AddScoped<StartWorkHandler>();
        services.AddScoped<CompleteWorkOrderHandler>();

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
        group.MapUpdateWorkOrder();
        group.MapAssignTechnician();
        group.MapUnassignTechnician();
        group.MapStartWork();
        group.MapCompleteWorkOrder();

        return app;
    }
}
