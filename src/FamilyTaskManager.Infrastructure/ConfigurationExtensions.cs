namespace FamilyTaskManager.Infrastructure;

public static class ConfigurationExtensions
{
  /// <summary>
  ///   Gets the database connection string with priority order:
  ///   1. "DefaultConnection" - standard ASP.NET Core connection string (used by Aspire, Docker, and local dev)
  ///   2. "SqliteConnection" - fallback to SQLite for testing
  /// </summary>
  /// <param name="configuration">The configuration instance</param>
  /// <returns>The database connection string</returns>
  /// <exception cref="InvalidOperationException">Thrown when no valid connection string is found</exception>
  public static string GetDatabaseConnectionString(this IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
      throw new InvalidOperationException(
        "No database connection string found. Tried: DefaultConnection");
    }

    return connectionString;
  }
}
