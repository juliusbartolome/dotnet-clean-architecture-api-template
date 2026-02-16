using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Template.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Template.IntegrationTests.Common;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string MsSqlImage = GetRequiredEnvironmentVariable("TESTCONTAINERS_MSSQL_IMAGE");
    private static readonly string RedisImage = GetRequiredEnvironmentVariable("TESTCONTAINERS_REDIS_IMAGE");
    private static readonly string MsSqlPassword = GetRequiredEnvironmentVariable("TESTCONTAINERS_MSSQL_PASSWORD");

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder(MsSqlImage)
        .WithPassword(MsSqlPassword)
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder(RedisImage)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}",
                ["Jwt:Issuer"] = "Template.Api",
                ["Jwt:Audience"] = "Template.Client",
                ["Jwt:SigningKey"] = "integration-tests-signing-key-at-least-32-characters"
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
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    private static string GetRequiredEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Required environment variable '{key}' was not provided.");
    }
}
