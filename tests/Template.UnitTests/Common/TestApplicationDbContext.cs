using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Abstractions;
using Template.Domain.Catalog;
using Template.Infrastructure.Persistence;

namespace Template.UnitTests.Common;

public sealed class TestApplicationDbContext : IApplicationDbContext, IDisposable
{
    private readonly TemplateDbContext _dbContext;

    public TestApplicationDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TemplateDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        _dbContext = new TemplateDbContext(options);
    }

    public DbSet<Product> Products => _dbContext.Products;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void Dispose() => _dbContext.Dispose();
}
