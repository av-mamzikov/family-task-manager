using FamilyTaskManager.Infrastructure.Data;

namespace FamilyTaskManager.Infrastructure.Interfaces;

public interface IQuartzSchemaInitializer
{
  /// <summary>
  ///   Initializes Quartz database schema if it doesn't already exist
  /// </summary>
  /// <param name="dbContext">The database context to use for schema operations</param>
  /// <param name="logger">Logger for recording initialization progress</param>
  Task InitializeAsync(AppDbContext dbContext, ILogger logger);
}
