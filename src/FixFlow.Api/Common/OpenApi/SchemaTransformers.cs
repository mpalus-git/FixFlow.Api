using FixFlow.Api.Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FixFlow.Api.Common.OpenApi;

public static class SchemaTransformers
{
    public static OpenApiOptions AddPreciseSchemaTypes(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, _) =>
        {
            var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
            if (type.IsEnum)
            {
                schema.Type = JsonSchemaType.String | (schema.Type.GetValueOrDefault() & JsonSchemaType.Null);
            }
            else if (type == typeof(decimal))
            {
                schema.Format = "decimal";
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static OpenApiOptions AddProblemDetailsErrorCode(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
            {
                return Task.CompletedTask;
            }

            schema.Description = "Error response in the RFC 9457 problem details format.";
            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[ErrorOrProblemExtensions.ErrorCodeExtension] = new OpenApiSchema
            {
                Type = JsonSchemaType.String | JsonSchemaType.Null,
                Description = "Stable machine-readable code of a business error, for example WorkOrder.NotFound. Absent for authentication and authorization failures.",
            };
            return Task.CompletedTask;
        });

        return options;
    }
}
