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
}
