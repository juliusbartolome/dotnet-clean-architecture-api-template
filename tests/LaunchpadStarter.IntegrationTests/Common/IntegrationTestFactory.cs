using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using LaunchpadStarter.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace LaunchpadStarter.IntegrationTests.Common;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultMsSqlImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string DefaultRedisImage = "redis:7-alpine";
    private const string DefaultMsSqlPassword = "Your_strong_password123!";
    private const string DefaultTestEnvironment = "IntegrationTesting";
    private const string DefaultJwtIssuer = "LaunchpadStarter.Api";
    private const string DefaultJwtAudience = "LaunchpadStarter.Client";
    private const string DefaultJwtSigningKey = "integration-tests-signing-key-at-least-32-characters";

    private static readonly string MsSqlImage = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_CONTAINERS_MSSQL_IMAGE", DefaultMsSqlImage);
    private static readonly string RedisImage = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_CONTAINERS_REDIS_IMAGE", DefaultRedisImage);
    private static readonly string MsSqlPassword = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_CONTAINERS_MSSQL_PASSWORD", DefaultMsSqlPassword);
    private static readonly string TestEnvironment = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_ENVIRONMENT", DefaultTestEnvironment);
    private static readonly string JwtIssuer = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_JWT_ISSUER", DefaultJwtIssuer);
    private static readonly string JwtAudience = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_JWT_AUDIENCE", DefaultJwtAudience);
    private static readonly string JwtSigningKey = GetEnvironmentVariableOrDefault("INTEGRATION_TEST_JWT_SIGNING_KEY", DefaultJwtSigningKey);

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder(MsSqlImage)
        .WithPassword(MsSqlPassword)
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder(RedisImage)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SigningKey"] = JwtSigningKey
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IConfigureOptions<AuthenticationOptions>));
            services.RemoveAll(typeof(IConfigureOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.RemoveAll(typeof(IDistributedCache));
            services.AddDistributedMemoryCache();
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        using var client = CreateClient();
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LaunchpadStarterDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    private static string GetEnvironmentVariableOrDefault(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
    }
}
