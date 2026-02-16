using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LaunchpadStarter.Infrastructure.Persistence;

public sealed class LaunchpadStarterDbContextFactory : IDesignTimeDbContextFactory<LaunchpadStarterDbContext>
{
    public LaunchpadStarterDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. Set environment variable 'ConnectionStrings__DefaultConnection'.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<LaunchpadStarterDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new LaunchpadStarterDbContext(optionsBuilder.Options);
    }
}
