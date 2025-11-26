using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FamilyTaskManager.Host;

public static class HostInitializationExtensions
{
  /// <summary>
  ///   Initializes infrastructure components (database migrations, schema, etc.)
  /// </summary>
  /// <param name="app">The web application</param>
  public static async Task InitializeInfrastructureAsync(this WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
      logger.LogInformation("Starting infrastructure initialization...");

      // Run EF Core migrations
      var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      await dbContext.Database.MigrateAsync();
      logger.LogInformation("Database migration completed");

      // Initialize Quartz schema
      var quartzInitializer = scope.ServiceProvider.GetRequiredService<IQuartzSchemaInitializer>();
      await quartzInitializer.InitializeAsync(dbContext, logger);

      logger.LogInformation("Infrastructure initialization completed successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Infrastructure initialization failed");
      throw;
    }
  }
}
