using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LaunchpadStarter.Infrastructure.Extensions;

namespace LaunchpadStarter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => LaunchpadStarter.Infrastructure.Extensions.ServiceCollectionExtensions.AddInfrastructure(services, configuration);
}
