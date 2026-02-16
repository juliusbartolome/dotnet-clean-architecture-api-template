using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Infrastructure.Caching;
using LaunchpadStarter.Infrastructure.Persistence;

namespace LaunchpadStarter.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LaunchpadStarterDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<LaunchpadStarterDbContext>());

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddScoped<ICacheVersionService, CacheVersionService>();

        services.AddHealthChecks()
            .AddDbContextCheck<LaunchpadStarterDbContext>(name: "database", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<RedisConnectivityHealthCheck>("redis", failureStatus: HealthStatus.Degraded);

        return services;
    }
}
