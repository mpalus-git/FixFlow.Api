using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixFlow.Api.IntegrationTests;

public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
