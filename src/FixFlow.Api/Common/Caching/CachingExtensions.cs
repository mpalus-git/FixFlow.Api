using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace FixFlow.Api.Common.Caching;

public static class CachingExtensions
{
    public const string RedisConnectionStringName = "Redis";

    private const int RedisTimeoutMilliseconds = 1000;

    public static IServiceCollection AddApplicationCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(5),
            LocalCacheExpiration = TimeSpan.FromMinutes(1),
        });

        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = CreateRedisOptions(redisConnectionString);
                options.InstanceName = "fixflow:";
            });
        }

        return services;
    }

    private static ConfigurationOptions CreateRedisOptions(string connectionString)
    {
        var redisOptions = ConfigurationOptions.Parse(connectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout = RedisTimeoutMilliseconds;
        redisOptions.SyncTimeout = RedisTimeoutMilliseconds;
        redisOptions.AsyncTimeout = RedisTimeoutMilliseconds;
        redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
        return redisOptions;
    }
}
