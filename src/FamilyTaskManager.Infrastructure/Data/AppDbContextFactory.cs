using Microsoft.EntityFrameworkCore.Design;

namespace FamilyTaskManager.Infrastructure.Data;

/// <summary>
///   Factory for creating DbContext at design time (for migrations)
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

    // Use PostgreSQL for migrations
    // Connection string will be provided at runtime, this is just for design-time
    optionsBuilder.UseNpgsql("Host=localhost;Database=FamilyTaskManager;Username=postgres;Password=postgres");

    return new AppDbContext(optionsBuilder.Options);
  }
}
