using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LaunchpadStarter.Infrastructure.Extensions;

public sealed class RedisConnectivityHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"health:redis:{Guid.NewGuid():N}";
            await cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var value = await cache.GetStringAsync(key, cancellationToken);
            if (value != "1")
            {
                return HealthCheckResult.Degraded("Redis cache read/write verification failed.");
            }

            await cache.RemoveAsync(key, cancellationToken);
            return HealthCheckResult.Healthy("Redis cache reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis cache is not reachable.", ex);
        }
    }
}
