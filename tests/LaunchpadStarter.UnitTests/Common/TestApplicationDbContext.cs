using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Application.Common.Abstractions;
using LaunchpadStarter.Domain.Catalog;
using LaunchpadStarter.Infrastructure.Persistence;

namespace LaunchpadStarter.UnitTests.Common;

public sealed class TestApplicationDbContext : IApplicationDbContext, IDisposable
{
    private readonly LaunchpadStarterDbContext _dbContext;

    public TestApplicationDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<LaunchpadStarterDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        _dbContext = new LaunchpadStarterDbContext(options);
    }

    public DbSet<Product> Products => _dbContext.Products;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void Dispose() => _dbContext.Dispose();
}
