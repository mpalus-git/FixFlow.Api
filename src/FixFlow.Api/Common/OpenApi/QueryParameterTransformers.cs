using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FixFlow.Api.Common.OpenApi;

public static class QueryParameterTransformers
{
    public static OpenApiOptions AddAllowedQueryValues(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, _) =>
        {
            foreach (var parameterDescription in context.Description.ParameterDescriptions)
            {
                if (parameterDescription.Source != BindingSource.Query
                    || parameterDescription.ParameterDescriptor is not IParameterInfoParameterDescriptor { ParameterInfo: var parameterInfo }
                    || parameterInfo.GetCustomAttribute<AllowedValuesAttribute>() is not { } allowedValues)
                {
                    continue;
                }

                var parameter = operation.Parameters?.SingleOrDefault(parameter => parameter.Name == parameterDescription.Name);
                if (parameter?.Schema is OpenApiSchema schema)
                {
                    schema.Enum = [.. allowedValues.Values.OfType<string>().Select(value => (JsonNode)JsonValue.Create(value))];
                }
            }

            return Task.CompletedTask;
        });

        return options;
    }
}
