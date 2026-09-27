using Asp.Versioning;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace FixFlow.Api.Common.OpenApi;

public static class ApiDocumentationExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options => options.Document
                .AddBearerSecurity()
                .AddPreciseSchemaTypes()
                .AddProblemDetailsErrorCode()
                .AddDocumentTransformer((document, _, _) =>
                {
                    document.Info.Title = "FixFlow API";
                    document.Info.Description = "Field service work order management API for dispatchers and technicians.";
                    return Task.CompletedTask;
                }));

        return services;
    }

    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference(options =>
        {
            foreach (var description in app.DescribeApiVersions())
            {
                options.AddDocument(description.GroupName, description.GroupName);
            }
        });

        return app;
    }
}
