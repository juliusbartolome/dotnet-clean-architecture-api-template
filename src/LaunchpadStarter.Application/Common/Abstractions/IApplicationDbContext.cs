using Microsoft.EntityFrameworkCore;
using LaunchpadStarter.Domain.Catalog;

namespace LaunchpadStarter.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
