using Microsoft.Extensions.Caching.Distributed;

namespace WebApplication1.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<string?> GetStringAsync(string key);
    Task SetRawAsync(string key, string value, TimeSpan? expiration = null);
}