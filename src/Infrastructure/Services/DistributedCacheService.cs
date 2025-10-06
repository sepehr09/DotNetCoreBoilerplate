using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using MyApp.Application.Common.Interfaces;

namespace MyApp.Infrastructure.Services;

public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _distributedCache;

    public DistributedCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        var value = await _distributedCache.GetStringAsync(key, token);

        if (value == null)
            return default;

        return JsonSerializer.Deserialize<T>(value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, serializedValue, options ?? new DistributedCacheEntryOptions(), token);
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await _distributedCache.RemoveAsync(key, token);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await _distributedCache.RefreshAsync(key, token);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken token = default)
    {
        var value = await _distributedCache.GetStringAsync(key, token);
        return !string.IsNullOrEmpty(value);
    }
}
