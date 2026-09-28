using FixFlow.Api.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.UnitTests.Common.Caching;

public sealed class CachingExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Register_Only_In_Memory_Hybrid_Cache_When_Redis_Connection_String_Is_Empty(string? redisConnectionString)
    {
        var services = new ServiceCollection().AddApplicationCaching(CreateConfiguration(redisConnectionString));

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(HybridCache));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(IDistributedCache));
    }

    [Fact]
    public void Should_Register_Redis_Distributed_Cache_When_Redis_Connection_String_Is_Provided()
    {
        var services = new ServiceCollection().AddApplicationCaching(CreateConfiguration("localhost:6379"));

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(HybridCache));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IDistributedCache));
    }

    [Theory]
    [InlineData(null, 1000)]
    [InlineData("00:00:00.100", 100)]
    public void Should_Apply_Redis_Timeouts_From_Configuration_Or_Default_When_Redis_Is_Configured(string? redisTimeout, int expectedMilliseconds)
    {
        using var serviceProvider = new ServiceCollection()
            .AddApplicationCaching(CreateConfiguration("localhost:6379", redisTimeout))
            .BuildServiceProvider();

        var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisCacheOptions>>().Value.ConfigurationOptions.ShouldNotBeNull();

        redisOptions.ConnectTimeout.ShouldBe(expectedMilliseconds);
        redisOptions.SyncTimeout.ShouldBe(expectedMilliseconds);
        redisOptions.AsyncTimeout.ShouldBe(expectedMilliseconds);
    }

    private static IConfiguration CreateConfiguration(string? redisConnectionString, string? redisTimeout = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{CachingExtensions.RedisConnectionStringName}"] = redisConnectionString,
                [CachingExtensions.RedisTimeoutKey] = redisTimeout,
            })
            .Build();
}
