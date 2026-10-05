using FixFlow.Api.Common.OpenApi;

namespace FixFlow.Api.Common.Time;

public sealed class ClientClockOptions
{
    public const string SectionName = "ClientClock";

    public static readonly TimeSpan MaxAllowedSkew = TimeSpan.FromHours(1);

    public TimeSpan MaxSkew { get; init; } = TimeSpan.FromMinutes(5);

    public bool IsValid() => MaxSkew >= TimeSpan.Zero && MaxSkew <= MaxAllowedSkew;
}

public static class ClientClockExtensions
{
    public static IServiceCollection AddClientClock(this IServiceCollection services)
    {
        services.AddOptions<ClientClockOptions>()
            .BindConfiguration(ClientClockOptions.SectionName)
            .Validate(options => options.IsValid(), "Client clock skew must be between zero and one hour.")
            .ValidateOnStartOutsideBuildTimeGeneration();

        return services;
    }
}
