using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace FixFlow.Api.Common.Caching;

public sealed partial class ResilientDistributedCache(IDistributedCache innerCache, ILogger<ResilientDistributedCache> logger)
    : IDistributedCache, IDisposable
{
    public byte[]? Get(string key) => Execute(nameof(Get), () => innerCache.Get(key));

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(nameof(GetAsync), () => innerCache.GetAsync(key, token));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        Execute(nameof(Set), () => innerCache.Set(key, value, options));

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        ExecuteAsync(nameof(SetAsync), () => innerCache.SetAsync(key, value, options, token));

    public void Refresh(string key) => Execute(nameof(Refresh), () => innerCache.Refresh(key));

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(nameof(RefreshAsync), () => innerCache.RefreshAsync(key, token));

    public void Remove(string key) => Execute(nameof(Remove), () => innerCache.Remove(key));

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        ExecuteAsync(nameof(RemoveAsync), () => innerCache.RemoveAsync(key, token));

    public void Dispose() => (innerCache as IDisposable)?.Dispose();

    private static bool IsUnavailability(Exception exception) => exception is RedisException or TimeoutException;

    private byte[]? Execute(string operation, Func<byte[]?> action)
    {
        try
        {
            return action();
        }
        catch (Exception exception) when (IsUnavailability(exception))
        {
            LogDistributedCacheUnavailable(operation, exception);
            return null;
        }
    }

    private void Execute(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (IsUnavailability(exception))
        {
            LogDistributedCacheUnavailable(operation, exception);
        }
    }

    private async Task<byte[]?> ExecuteAsync(string operation, Func<Task<byte[]?>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (IsUnavailability(exception))
        {
            LogDistributedCacheUnavailable(operation, exception);
            return null;
        }
    }

    private async Task ExecuteAsync(string operation, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception) when (IsUnavailability(exception))
        {
            LogDistributedCacheUnavailable(operation, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Distributed cache operation {Operation} failed, continuing with the in-memory cache only")]
    private partial void LogDistributedCacheUnavailable(string operation, Exception exception);
}
