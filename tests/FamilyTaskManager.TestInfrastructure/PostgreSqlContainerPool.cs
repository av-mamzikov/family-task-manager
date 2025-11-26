using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FamilyTaskManager.TestInfrastructure;

/// <summary>
///   Пул контейнеров PostgreSQL для повторного использования в тестах.
///   При первом обращении создаётся контейнер с template базой данных (мигрированной).
///   При повторном обращении рабочая база удаляется и копируется заново из template.
///   Если все контейнеры заняты - создаётся новый контейнер и добавляется в пул.
/// </summary>
/// <typeparam name="TDbContext">Тип DbContext для применения миграций</typeparam>
public sealed class PostgreSqlContainerPool<TDbContext> : IAsyncDisposable
  where TDbContext : DbContext
{
  private const string TemplateDatabaseName = "template_test_db";
  private static readonly SemaphoreSlim _lock = new(1, 1);
  private static PostgreSqlContainerPool<TDbContext>? _instance;
  private static int _nextDatabaseNumber = 1;
  private readonly ConcurrentBag<PooledContainer> _allContainers = [];

  private readonly ConcurrentBag<PooledContainer> _availableContainers = [];

  private PostgreSqlContainerPool()
  {
  }

  /// <summary>
  ///   Получает единственный экземпляр пула контейнеров
  /// </summary>
  public static PostgreSqlContainerPool<TDbContext> Instance
  {
    get
    {
      if (_instance == null)
      {
        _lock.Wait();
        try
        {
          _instance ??= new PostgreSqlContainerPool<TDbContext>();
        }
        finally
        {
          _lock.Release();
        }
      }

      return _instance;
    }
  }

  /// <summary>
  ///   Очищает пул и останавливает все контейнеры
  /// </summary>
  public async ValueTask DisposeAsync()
  {
    await _lock.WaitAsync();
    try
    {
      while (_allContainers.TryTake(out var container))
      {
        await container.Container.DisposeAsync();
      }

      _availableContainers.Clear();
      _instance = null;
    }
    finally
    {
      _lock.Release();
    }
  }

  /// <summary>
  ///   Получает контейнер из пула или создаёт новый
  /// </summary>
  public async Task<PooledContainer> AcquireContainerAsync()
  {
    // Пытаемся получить свободный контейнер из пула (он уже сброшен)
    if (_availableContainers.TryTake(out var container))
    {
      container.MarkAsInUse();
      return container;
    }

    // Если свободных контейнеров нет - создаём новый
    await _lock.WaitAsync();
    try
    {
      var newContainer = await CreateNewContainerAsync();
      _allContainers.Add(newContainer);
      newContainer.MarkAsInUse();
      return newContainer;
    }
    finally
    {
      _lock.Release();
    }
  }

  /// <summary>
  ///   Возвращает контейнер в пул для повторного использования.
  ///   Сбрасывает базу данных перед возвратом в пул.
  /// </summary>
  public async Task ReleaseContainerAsync(PooledContainer container)
  {
    // Сбрасываем базу данных перед возвратом в пул
    await ResetDatabaseAsync(container);

    container.MarkAsAvailable();
    _availableContainers.Add(container);
  }

  /// <summary>
  ///   Создаёт новый контейнер, запускает его, создаёт template базу с миграциями и рабочую базу
  /// </summary>
  private async Task<PooledContainer> CreateNewContainerAsync()
  {
    var dbNumber = Interlocked.Increment(ref _nextDatabaseNumber);
    var workingDatabaseName = $"test_db_{dbNumber}";

    // Создаём контейнер с временной базой данных
    var container = new PostgreSqlBuilder()
      .WithImage("postgres:17-alpine")
      .WithDatabase("postgres")
      .WithUsername("postgres")
      .WithPassword("Test_password123!")
      .Build();

    await container.StartAsync();

    // Создаём template базу и применяем миграции
    await CreateTemplateDatabaseAsync(container);

    // Создаём рабочую базу из template
    await CreateWorkingDatabaseFromTemplateAsync(container, workingDatabaseName);

    return new PooledContainer(container, workingDatabaseName);
  }

  /// <summary>
  ///   Создаёт template базу данных и применяет миграции
  /// </summary>
  private static async Task CreateTemplateDatabaseAsync(PostgreSqlContainer container)
  {
    var postgresConnectionString = container.GetConnectionString();

    // Создаём template базу
    await using (var connection = new NpgsqlConnection(postgresConnectionString))
    {
      await connection.OpenAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = $"CREATE DATABASE {TemplateDatabaseName}";
      await command.ExecuteNonQueryAsync();
    }

    // Применяем миграции к template базе
    var templateConnectionString = new NpgsqlConnectionStringBuilder(postgresConnectionString)
    {
      Database = TemplateDatabaseName
    }.ConnectionString;

    var options = new DbContextOptionsBuilder<TDbContext>()
      .UseNpgsql(templateConnectionString)
      .Options;

    await using (var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options)!)
    {
      await dbContext.Database.MigrateAsync();
    }

    // Закрываем все соединения с template базой и помечаем её как template
    await using (var connection = new NpgsqlConnection(postgresConnectionString))
    {
      await connection.OpenAsync();

      // Отключаем все активные соединения с template базой
      await using (var command = connection.CreateCommand())
      {
        command.CommandText = $"""
                               SELECT pg_terminate_backend(pid)
                               FROM pg_stat_activity
                               WHERE datname = '{TemplateDatabaseName}'
                               """;
        await command.ExecuteNonQueryAsync();
      }

      // Помечаем базу как template (это предотвратит случайные подключения)
      await using (var command = connection.CreateCommand())
      {
        command.CommandText = $"""
                               UPDATE pg_database 
                               SET datistemplate = TRUE
                               WHERE datname = '{TemplateDatabaseName}'
                               """;
        await command.ExecuteNonQueryAsync();
      }
    }
  }

  /// <summary>
  ///   Создаёт рабочую базу данных путём копирования из template
  /// </summary>
  private static async Task CreateWorkingDatabaseFromTemplateAsync(
    PostgreSqlContainer container,
    string workingDatabaseName)
  {
    var postgresConnectionString = container.GetConnectionString();

    await using var connection = new NpgsqlConnection(postgresConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = $"CREATE DATABASE {workingDatabaseName} WITH TEMPLATE {TemplateDatabaseName}";
    await command.ExecuteNonQueryAsync();
  }

  /// <summary>
  ///   Сбрасывает рабочую базу данных путём её удаления и копирования из template
  /// </summary>
  private static async Task ResetDatabaseAsync(PooledContainer pooledContainer)
  {
    // Очищаем пул соединений Npgsql для рабочей базы данных
    var workingConnectionString = pooledContainer.GetConnectionString();
    NpgsqlConnection.ClearPool(new NpgsqlConnection(workingConnectionString));

    var postgresConnectionString = new NpgsqlConnectionStringBuilder(pooledContainer.Container.GetConnectionString())
    {
      Database = "postgres"
    }.ConnectionString;

    await using var connection = new NpgsqlConnection(postgresConnectionString);
    await connection.OpenAsync();

    // Удаляем рабочую базу (WITH FORCE автоматически закрывает все соединения в PostgreSQL 13+)
    await using (var command = connection.CreateCommand())
    {
      command.CommandText = $"DROP DATABASE IF EXISTS {pooledContainer.WorkingDatabaseName} WITH (FORCE)";
      await command.ExecuteNonQueryAsync();
    }

    // Создаём рабочую базу заново из template
    await using (var command = connection.CreateCommand())
    {
      command.CommandText =
        $"CREATE DATABASE {pooledContainer.WorkingDatabaseName} WITH TEMPLATE {TemplateDatabaseName}";
      await command.ExecuteNonQueryAsync();
    }
  }

  /// <summary>
  ///   Сбрасывает пул (для тестирования)
  /// </summary>
  internal static async Task ResetInstanceAsync()
  {
    if (_instance != null)
    {
      await _instance.DisposeAsync();
    }
  }
}

/// <summary>
///   Обёртка над контейнером PostgreSQL с информацией о состоянии
/// </summary>
public sealed class PooledContainer
{
  private int _isInUse;

  public PooledContainer(
    PostgreSqlContainer container,
    string workingDatabaseName)
  {
    Container = container;
    WorkingDatabaseName = workingDatabaseName;
  }

  public PostgreSqlContainer Container { get; }
  public string WorkingDatabaseName { get; }
  public bool IsInUse => Interlocked.CompareExchange(ref _isInUse, 0, 0) == 1;

  internal void MarkAsInUse() => Interlocked.Exchange(ref _isInUse, 1);

  internal void MarkAsAvailable() => Interlocked.Exchange(ref _isInUse, 0);

  /// <summary>
  ///   Получает строку подключения к рабочей базе данных
  /// </summary>
  public string GetConnectionString()
  {
    var baseConnectionString = Container.GetConnectionString();
    // Парсим строку подключения и заменяем имя базы данных
    var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
    {
      Database = WorkingDatabaseName
    };
    return builder.ConnectionString;
  }
}
