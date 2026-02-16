using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Domain.Catalog;

namespace LaunchpadStarter.Infrastructure.Persistence;

public sealed class LaunchpadStarterDbContext(DbContextOptions<LaunchpadStarterDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LaunchpadStarterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
