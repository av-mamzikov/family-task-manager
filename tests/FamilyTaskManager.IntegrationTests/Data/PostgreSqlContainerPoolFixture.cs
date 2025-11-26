using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.TestInfrastructure;

namespace FamilyTaskManager.IntegrationTests.Data;

/// <summary>
///   Фикстура для управления жизненным циклом пула контейнеров PostgreSQL.
///   Используется xUnit для очистки пула после завершения всех тестов.
/// </summary>
public sealed class PostgreSqlContainerPoolFixture : IAsyncLifetime
{
  public Task InitializeAsync() => Task.CompletedTask;

  public async Task DisposeAsync() => await PostgreSqlContainerPool<AppDbContext>.Instance.DisposeAsync();
}

/// <summary>
///   Коллекция для группировки всех интеграционных тестов,
///   использующих пул контейнеров PostgreSQL
/// </summary>
[CollectionDefinition(nameof(PostgreSqlContainerPoolCollection))]
public class PostgreSqlContainerPoolCollection : ICollectionFixture<PostgreSqlContainerPoolFixture>
{
}
