using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.TestInfrastructure;

namespace FamilyTaskManager.IntegrationTests.Data;

/// <summary>
///   Базовый класс для тестирования репозиториев с использованием пула контейнеров PostgreSQL.
///   xUnit создаёт новый экземпляр класса для каждого теста, поэтому каждый тест получает свой контейнер.
/// </summary>
[Collection(nameof(PostgreSqlContainerPoolCollection))]
public abstract class BaseRepositoryTestFixture : IAsyncLifetime
{
  private PooledContainer _pooledContainer = null!;

  protected AppDbContext DbContext { get; private set; } = null!;

  /// <summary>
  ///   Инициализация перед каждым тестом - получаем контейнер из пула
  /// </summary>
  public async Task InitializeAsync()
  {
    // Получаем контейнер из пула
    _pooledContainer = await PostgreSqlContainerPool<AppDbContext>.Instance.AcquireContainerAsync();

    // Создаём DbContext
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_pooledContainer.GetConnectionString())
      .EnableSensitiveDataLogging()
      .Options;

    DbContext = new(options);
  }

  /// <summary>
  ///   Очистка после каждого теста - закрываем DbContext и возвращаем контейнер в пул
  /// </summary>
  public async Task DisposeAsync()
  {
    // Явно закрываем соединение с базой данных
    await DbContext.Database.CloseConnectionAsync();
    await DbContext.DisposeAsync();

    // Возвращаем контейнер в пул для повторного использования (с предварительным сбросом БД)
    await PostgreSqlContainerPool<AppDbContext>.Instance.ReleaseContainerAsync(_pooledContainer);
  }

  /// <summary>
  ///   Создает репозиторий для указанного типа агрегата
  /// </summary>
  protected IAppRepository<T> GetRepository<T>() where T : class, IAggregateRoot => new EfAppRepository<T>(DbContext);

  /// <summary>
  ///   Создает read-only репозиторий для указанного типа агрегата
  /// </summary>
  protected IAppReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot =>
    new EfAppRepository<T>(DbContext);

  /// <summary>
  ///   Очищает все данные из БД между тестами (опционально)
  /// </summary>
  protected async Task ClearDatabaseAsync() =>
    await DbContext.Database.ExecuteSqlRawAsync(
      "TRUNCATE TABLE \"Families\", \"FamilyMembers\", \"Users\", \"Spots\", \"TaskTemplates\", \"TaskInstances\", \"ActionHistory\" CASCADE");
}
