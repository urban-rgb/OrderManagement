using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Services;

[ExcludeFromCodeCoverage]
public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await cache.GetStringAsync(key);
            return string.IsNullOrWhiteSpace(data) ? default : JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            logger.LogWarning(ex, "Cache read error for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            logger.LogWarning(ex, "Cache write error for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache remove error for key {Key}", key);
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            return await cache.GetStringAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache read string error for key {Key}", key);
            return null;
        }
    }

    public async Task SetRawAsync(string key, string value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await cache.SetStringAsync(key, value, options);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache set string error for key {Key}", key);
        }
    }
}