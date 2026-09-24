using FixFlow.Api.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace FixFlow.Api.UnitTests.Common.Caching;

public sealed class ResilientDistributedCacheTests : IDisposable
{
    private const string Key = "clients:list:1:20:";

    private static readonly RedisConnectionException ConnectionFailure = new(ConnectionFailureType.UnableToConnect, "Redis is unavailable.");

    private readonly IDistributedCache _innerCache = Substitute.For<IDistributedCache>();
    private readonly ResilientDistributedCache _cache;

    public ResilientDistributedCacheTests()
    {
        _cache = new ResilientDistributedCache(_innerCache, NullLogger<ResilientDistributedCache>.Instance);
    }

    [Fact]
    public async Task Should_Return_Value_From_Redis_When_Redis_Is_Available()
    {
        byte[] value = [1, 2, 3];
        _innerCache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns(value);

        var result = await _cache.GetAsync(Key, TestContext.Current.CancellationToken);

        result.ShouldBe(value);
    }

    [Fact]
    public async Task Should_Return_Cache_Miss_When_Redis_Connection_Fails_On_Read()
    {
        _innerCache.GetAsync(Key, Arg.Any<CancellationToken>()).ThrowsAsync(ConnectionFailure);

        var result = await _cache.GetAsync(Key, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Ignore_Write_When_Redis_Times_Out()
    {
        var timeout = new RedisTimeoutException("Redis timed out.", CommandStatus.Sent);
        _innerCache.SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>()).ThrowsAsync(timeout);

        await Should.NotThrowAsync(() => _cache.SetAsync(Key, [1], new DistributedCacheEntryOptions(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Should_Ignore_Remove_When_Redis_Connection_Fails()
    {
        _innerCache.When(cache => cache.Remove(Key)).Throw(ConnectionFailure);

        Should.NotThrow(() => _cache.Remove(Key));
    }

    [Fact]
    public async Task Should_Propagate_Cancellation_When_Operation_Is_Cancelled()
    {
        _innerCache.GetAsync(Key, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _cache.GetAsync(Key, TestContext.Current.CancellationToken));
    }

    public void Dispose() => _cache.Dispose();
}
