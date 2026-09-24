using FixFlow.Api.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    private static IConfiguration CreateConfiguration(string? redisConnectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{CachingExtensions.RedisConnectionStringName}"] = redisConnectionString,
            })
            .Build();
}
