using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FixFlow.Api.Common.Caching;

public static class CachingExtensions
{
    public const string RedisConnectionStringName = "Redis";

    public const string RedisTimeoutKey = "Caching:RedisTimeout";

    private static readonly TimeSpan DefaultRedisTimeout = TimeSpan.FromSeconds(1);

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
            var redisTimeout = configuration.GetValue(RedisTimeoutKey, DefaultRedisTimeout);
            services.Configure<RedisCacheOptions>(options =>
            {
                options.ConfigurationOptions = CreateRedisOptions(redisConnectionString, redisTimeout);
                options.InstanceName = "fixflow:";
            });
            services.AddSingleton<IDistributedCache>(serviceProvider => new ResilientDistributedCache(
                new RedisCache(serviceProvider.GetRequiredService<IOptions<RedisCacheOptions>>()),
                serviceProvider.GetRequiredService<ILogger<ResilientDistributedCache>>()));
        }

        return services;
    }

    private static ConfigurationOptions CreateRedisOptions(string connectionString, TimeSpan timeout)
    {
        var timeoutMilliseconds = (int)timeout.TotalMilliseconds;
        var redisOptions = ConfigurationOptions.Parse(connectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout = timeoutMilliseconds;
        redisOptions.SyncTimeout = timeoutMilliseconds;
        redisOptions.AsyncTimeout = timeoutMilliseconds;
        redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
        return redisOptions;
    }
}
