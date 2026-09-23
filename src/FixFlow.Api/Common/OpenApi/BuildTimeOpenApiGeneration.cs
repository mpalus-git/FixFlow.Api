using System.Reflection;

namespace FixFlow.Api.Common.OpenApi;

public static class BuildTimeOpenApiGeneration
{
    public static bool IsRunning => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
