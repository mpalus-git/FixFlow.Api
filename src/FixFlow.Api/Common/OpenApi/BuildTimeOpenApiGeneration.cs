using System.Reflection;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Common.OpenApi;

public static class BuildTimeOpenApiGeneration
{
    public static bool IsRunning => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static OptionsBuilder<TOptions> ValidateOnStartOutsideBuildTimeGeneration<TOptions>(this OptionsBuilder<TOptions> builder)
        where TOptions : class =>
        IsRunning ? builder : builder.ValidateOnStart();
}
