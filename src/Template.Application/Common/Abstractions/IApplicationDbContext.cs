using Microsoft.EntityFrameworkCore;
using Template.Domain.Catalog;

namespace Template.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
