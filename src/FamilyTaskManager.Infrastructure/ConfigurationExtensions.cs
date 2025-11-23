namespace FamilyTaskManager.Infrastructure;

public static class ConfigurationExtensions
{
  /// <summary>
  ///   Gets the database connection string with priority order:
  ///   1. "FamilyTaskManager" - provided by Aspire when using .WithReference(cleanArchDb)
  ///   2. "DefaultConnection" - traditional PostgreSQL connection
  ///   3. "SqliteConnection" - fallback to SQLite
  /// </summary>
  /// <param name="configuration">The configuration instance</param>
  /// <returns>The database connection string</returns>
  /// <exception cref="InvalidOperationException">Thrown when no valid connection string is found</exception>
  public static string GetDatabaseConnectionString(this IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("FamilyTaskManager");

    if (string.IsNullOrEmpty(connectionString))
    {
      throw new InvalidOperationException(
        "No database connection string found. Tried: FamilyTaskManager, DefaultConnection, SqliteConnection");
    }

    return connectionString;
  }
}
