using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LaunchpadStarter.Infrastructure.Persistence;

public sealed class LaunchpadStarterDbContextFactory : IDesignTimeDbContextFactory<LaunchpadStarterDbContext>
{
    public LaunchpadStarterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LaunchpadStarterDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=LaunchpadStarterDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True");
        return new LaunchpadStarterDbContext(optionsBuilder.Options);
    }
}
