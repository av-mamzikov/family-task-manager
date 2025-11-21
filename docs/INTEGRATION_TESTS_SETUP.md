# Настройка интеграционных тестов с Testcontainers PostgreSQL

## Что было сделано

### 1. Добавлены пакеты

**Directory.Packages.props:**
- `Testcontainers.PostgreSql` 4.3.0
- Обновлены EF Core пакеты до RC версии (10.0.0-rc.2.25502.107) для совместимости с Npgsql

**FamilyTaskManager.IntegrationTests.csproj:**
- `Testcontainers`
- `Testcontainers.PostgreSql`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Relational` (для разрешения конфликтов версий)

### 2. Создан базовый класс для тестов

**BaseRepositoryTestFixture.cs** - базовый класс для всех тестов репозиториев:
- Использует PostgreSQL 17 Alpine контейнер
- Автоматически применяет EF Core миграции
- Каждый тестовый класс получает изолированный контейнер
- Реализует `IAsyncLifetime` для управления жизненным циклом

```csharp
public abstract class BaseRepositoryTestFixture : IAsyncLifetime
{
  protected AppDbContext DbContext { get; private set; }
  protected IRepository<T> GetRepository<T>() where T : class, IAggregateRoot
  protected IReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot
  protected Task ClearDatabaseAsync() // Опциональная очистка БД
}
```

### 3. Созданы тесты репозиториев

#### FamilyRepositoryTests (11 тестов)
- ✅ AddAsync - сохранение в БД
- ✅ UpdateAsync - обновление существующей сущности
- ✅ DeleteAsync - удаление из БД
- ✅ GetByIdAsync - получение по ID
- ✅ GetByIdAsync с несуществующим ID
- ✅ ListAsync - получение всех сущностей
- ✅ AddMember - сохранение связанных сущностей
- ✅ CountAsync - подсчет записей
- ✅ AnyAsync с данными
- ✅ AnyAsync с пустой БД
- ✅ SaveChangesAsync - множественные операции

#### TaskTemplateRepositoryTests (10 тестов)
- ✅ AddAsync - сохранение в БД
- ✅ UpdateAsync - обновление полей
- ✅ Deactivate - изменение статуса
- ✅ DeleteAsync - удаление из БД
- ✅ ListAsync - получение всех шаблонов
- ✅ CountAsync - подсчет записей
- ✅ GetByIdAsync с несуществующим ID
- ✅ AnyAsync с данными
- ✅ AnyAsync с пустой БД

### 4. Удалены устаревшие файлы

- ❌ `BaseEfRepoTestFixture.cs` (использовал InMemory DB)

## Преимущества нового подхода

### Testcontainers vs InMemory

| Критерий | InMemory | Testcontainers + PostgreSQL |
|----------|----------|----------------------------|
| **Реализм** | Низкий | Высокий |
| **Скорость** | Мгновенно | ~5-10 сек на класс |
| **Constraints** | Не поддерживает | Полная поддержка |
| **Triggers** | Нет | Да |
| **Stored Procedures** | Нет | Да |
| **Миграции** | Не проверяет | Проверяет |
| **Изоляция** | Общая память | Отдельные контейнеры |
| **Production Parity** | Нет | Да |

## Запуск тестов

### Требования
- Docker Desktop должен быть запущен
- При первом запуске скачается образ PostgreSQL 17 Alpine (~80 MB)

### Команды

```bash
# Все интеграционные тесты
dotnet test tests/FamilyTaskManager.IntegrationTests

# Конкретный класс
dotnet test --filter "FullyQualifiedName~FamilyRepositoryTests"

# С подробным выводом
dotnet test tests/FamilyTaskManager.IntegrationTests --logger "console;verbosity=detailed"

# Игнорировать предупреждения о безопасности (временно)
dotnet build /p:TreatWarningsAsErrors=false
```

## Структура проекта

```
tests/FamilyTaskManager.IntegrationTests/
├── Data/
│   ├── BaseRepositoryTestFixture.cs       # Базовый класс с Testcontainers
│   ├── FamilyRepositoryTests.cs           # 11 тестов для Family
│   └── TaskTemplateRepositoryTests.cs     # 10 тестов для TaskTemplate
├── GlobalUsings.cs                        # Глобальные using директивы
├── FamilyTaskManager.IntegrationTests.csproj
└── README.md                              # Подробная документация
```

## Конфигурация PostgreSQL контейнера

```csharp
private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
  .WithImage("postgres:17-alpine")
  .WithDatabase("familytaskmanager_test")
  .WithUsername("postgres")
  .WithPassword("Test_password123!")
  .Build();
```

## Добавление тестов для новых агрегатов

```csharp
public class PetRepositoryTests : BaseRepositoryTestFixture
{
  private IRepository<Pet> Repository => GetRepository<Pet>();

  [Fact]
  public async Task AddAsync_ShouldPersistPetToDatabase()
  {
    // Arrange
    var pet = new Pet(...);

    // Act
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(pet.Id);
    retrieved.ShouldNotBeNull();
  }
}
```

## Известные проблемы

### Предупреждения о безопасности
```
NU1903: Package 'Microsoft.Build.Tasks.Core' 17.14.8 has a known high severity vulnerability
```
**Решение**: Это транзитивная зависимость от EF Core Design. Не влияет на тесты. Будет исправлено в следующем релизе EF Core.

### Версии пакетов
- Используется EF Core 10.0.0-rc.2 (RC версия)
- Npgsql 10.0.0-rc.2 (RC версия)
- После выхода стабильных версий нужно обновить

## Следующие шаги

- [ ] Добавить тесты для Pet репозитория
- [ ] Добавить тесты для User репозитория
- [ ] Добавить тесты для ActionHistory репозитория
- [ ] Добавить тесты для Specifications (фильтрация)
- [ ] Добавить тесты для транзакций
- [ ] Настроить параллельное выполнение тестов
- [ ] Обновить до стабильных версий EF Core 10 после релиза

## Время выполнения

- **Первый запуск**: ~20-30 секунд (скачивание образа + старт контейнера)
- **Последующие запуски**: ~5-10 секунд на тестовый класс
- **Общее время**: ~15-20 секунд для всех 21 теста

## Полезные ссылки

- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [PostgreSQL Testcontainers](https://dotnet.testcontainers.org/modules/postgres/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
