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
        var databaseProvider = configuration["Database:Provider"] ?? "SqlServer";
        var provider = databaseProvider.Trim().ToLowerInvariant();

        services.AddDbContext<TemplateDbContext>(options =>
        {
            switch (provider)
            {
                case "sqlserver":
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

                    options.UseSqlServer(connectionString, sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    break;
                }
                case "sqlite":
                {
                    var sqliteConnection = configuration.GetConnectionString("Sqlite") ?? "Data Source=template.db";
                    options.UseSqlite(sqliteConnection);
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider '{databaseProvider}'. Supported values are 'SqlServer' and 'Sqlite'.");
            }
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TemplateDbContext>());

        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddScoped<ICacheVersionService, CacheVersionService>();

        services.AddHealthChecks()
            .AddDbContextCheck<TemplateDbContext>(name: "database", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<RedisConnectivityHealthCheck>("redis", failureStatus: HealthStatus.Degraded);

        return services;
    }
}
