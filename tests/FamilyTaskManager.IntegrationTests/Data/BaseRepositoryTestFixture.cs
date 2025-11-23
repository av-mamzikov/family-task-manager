using FamilyTaskManager.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace FamilyTaskManager.IntegrationTests.Data;

/// <summary>
///   Базовый класс для тестирования репозиториев с использованием Testcontainers PostgreSQL.
///   Каждый тестовый класс получает свой изолированный контейнер с БД.
/// </summary>
public abstract class BaseRepositoryTestFixture : IAsyncLifetime
{
  private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
    .WithImage("postgres:17-alpine")
    .WithDatabase("familytaskmanager_test")
    .WithUsername("postgres")
    .WithPassword("Test_password123!")
    .Build();

  protected AppDbContext DbContext { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    await _dbContainer.StartAsync();

    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_dbContainer.GetConnectionString())
      .Options;

    DbContext = new AppDbContext(options);

    // Применяем миграции
    await DbContext.Database.MigrateAsync();
  }

  public async Task DisposeAsync()
  {
    await DbContext.DisposeAsync();
    await _dbContainer.DisposeAsync();
  }

  /// <summary>
  ///   Создает репозиторий для указанного типа агрегата
  /// </summary>
  protected IRepository<T> GetRepository<T>() where T : class, IAggregateRoot
  {
    return new EfRepository<T>(DbContext);
  }

  /// <summary>
  ///   Создает read-only репозиторий для указанного типа агрегата
  /// </summary>
  protected IReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot
  {
    return new EfRepository<T>(DbContext);
  }

  /// <summary>
  ///   Очищает все данные из БД между тестами (опционально)
  /// </summary>
  protected async Task ClearDatabaseAsync()
  {
    await DbContext.Database.ExecuteSqlRawAsync(
      "TRUNCATE TABLE \"Families\", \"FamilyMembers\", \"Users\", \"Pets\", \"TaskTemplates\", \"TaskInstances\", \"ActionHistory\" CASCADE");
  }
}
