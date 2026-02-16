using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using LaunchpadStarter.Application.Common.Abstractions;

namespace LaunchpadStarter.Infrastructure.Caching;

public sealed class DistributedCacheService(IDistributedCache distributedCache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => distributedCache.RemoveAsync(key, cancellationToken);
}
