using FixFlow.Api.Features.Parts.ArchivePart;
using FixFlow.Api.Features.Parts.CreatePart;
using FixFlow.Api.Features.Parts.GetPart;
using FixFlow.Api.Features.Parts.ListParts;
using FixFlow.Api.Features.Parts.UpdatePart;
using FluentValidation;

namespace FixFlow.Api.Features.Parts;

public static class PartsModule
{
    public static IServiceCollection AddPartsFeatures(this IServiceCollection services)
    {
        services.AddScoped<CreatePartHandler>();
        services.AddSingleton<IValidator<CreatePartRequest>, CreatePartRequestValidator>();
        services.AddScoped<GetPartHandler>();
        services.AddScoped<ListPartsHandler>();
        services.AddSingleton<IValidator<ListPartsRequest>, ListPartsRequestValidator>();
        services.AddScoped<UpdatePartHandler>();
        services.AddSingleton<IValidator<UpdatePartRequest>, UpdatePartRequestValidator>();
        services.AddScoped<ArchivePartHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapPartsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Parts")
            .MapGroup("/api/v{version:apiVersion}/parts")
            .HasApiVersion(1)
            .WithTags("Parts")
            .RequireAuthorization();

        group.MapCreatePart();
        group.MapGetPart();
        group.MapListParts();
        group.MapUpdatePart();
        group.MapArchivePart();

        return app;
    }
}
