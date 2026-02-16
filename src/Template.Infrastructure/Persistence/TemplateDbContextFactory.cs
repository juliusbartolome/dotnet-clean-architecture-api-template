using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Template.Infrastructure.Persistence;

public sealed class TemplateDbContextFactory : IDesignTimeDbContextFactory<TemplateDbContext>
{
    public TemplateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TemplateDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=TemplateDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True");
        return new TemplateDbContext(optionsBuilder.Options);
    }
}
