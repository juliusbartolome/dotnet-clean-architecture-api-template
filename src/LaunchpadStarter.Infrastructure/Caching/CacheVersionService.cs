using Microsoft.Extensions.Caching.Distributed;
using LaunchpadStarter.Application.Common.Abstractions;

namespace LaunchpadStarter.Infrastructure.Caching;

public sealed class CacheVersionService(IDistributedCache cache) : ICacheVersionService
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(30);

    public async Task<string> GetVersionAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = "v1";
            await cache.SetStringAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VersionTtl }, cancellationToken);
        }

        return value;
    }

    public async Task<string> BumpVersionAsync(string key, CancellationToken cancellationToken = default)
    {
        var newVersion = $"v{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await cache.SetStringAsync(key, newVersion, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = VersionTtl }, cancellationToken);
        return newVersion;
    }
}
