using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace MyApp.Application.Common.Interfaces;

public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);
    Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
    Task RefreshAsync(string key, CancellationToken token = default);
    Task<bool> ExistsAsync(string key, CancellationToken token = default);
}
