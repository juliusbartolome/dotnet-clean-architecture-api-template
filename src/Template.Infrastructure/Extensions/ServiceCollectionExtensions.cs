using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Template.Application.Common.Abstractions;
using Template.Infrastructure.Caching;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<TemplateDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TemplateDbContext>());

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddScoped<ICacheVersionService, CacheVersionService>();

        services.AddHealthChecks()
            .AddDbContextCheck<TemplateDbContext>(name: "sqlserver", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<RedisConnectivityHealthCheck>("redis", failureStatus: HealthStatus.Degraded);

        return services;
    }
}
