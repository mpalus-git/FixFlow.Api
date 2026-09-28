using FixFlow.Api.Common.Concurrency;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace FixFlow.Api.Common.OpenApi;

public static class ConditionalRequestTransformers
{
    public static OpenApiOptions AddConditionalRequestHeaders(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, _) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IfMatchRequiredMetadata>().Any())
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = HeaderNames.IfMatch,
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "ETag of the resource returned by the last read or change. A different current version is rejected with 412.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                });
            }

            if (metadata.OfType<ETagResponseMetadata>().Any() && operation.Responses is not null)
            {
                foreach (var (statusCode, response) in operation.Responses)
                {
                    if (statusCode.StartsWith('2') && response is OpenApiResponse successResponse)
                    {
                        successResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
                        successResponse.Headers[HeaderNames.ETag] = new OpenApiHeader
                        {
                            Description = "Current version of the resource, to be sent in If-Match when changing it.",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                        };
                    }
                }
            }

            return Task.CompletedTask;
        });

        return options;
    }
}
