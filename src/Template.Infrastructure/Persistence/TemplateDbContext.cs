using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Abstractions;
using Template.Domain.Catalog;

namespace Template.Infrastructure.Persistence;

public sealed class TemplateDbContext(DbContextOptions<TemplateDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TemplateDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
