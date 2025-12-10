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
  protected TestRepositoryFactory RepositoryFactory = null!;
  protected AppDbContext DbContext { get; private set; } = null!;

  /// <summary>
  ///   Инициализация перед каждым тестом - получаем контейнер из пула
  /// </summary>
  public async Task InitializeAsync()
  {
    // Получаем контейнер из пула
    _pooledContainer = await PostgreSqlContainerPool<AppDbContext>.Instance.AcquireContainerAsync();

    // Создаём DbContext
    DbContext = _pooledContainer.GetDbContext();
    RepositoryFactory = new(DbContext);
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
}
