# Integration Tests - Repository Layer

## Обзор

Интеграционные тесты для репозиториев используют **Testcontainers** с **PostgreSQL** для реалистичного тестирования слоя данных.

## Архитектура

### BaseRepositoryTestFixture

Базовый класс для всех тестов репозиториев:
- Автоматически создает изолированный PostgreSQL контейнер для каждого тестового класса
- Применяет EF Core миграции
- Предоставляет методы для получения репозиториев
- Управляет жизненным циклом контейнера через `IAsyncLifetime`

```csharp
public abstract class BaseRepositoryTestFixture : IAsyncLifetime
{
  protected AppDbContext DbContext { get; private set; }
  protected IRepository<T> GetRepository<T>() where T : class, IAggregateRoot
  protected IReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot
}
```

### Преимущества Testcontainers

✅ **Реальная БД**: Тесты выполняются на настоящем PostgreSQL, а не InMemory  
✅ **Изоляция**: Каждый тестовый класс получает свой контейнер  
✅ **Миграции**: Проверяется корректность EF Core миграций  
✅ **Production Parity**: Поведение БД идентично продакшену  
✅ **Автоочистка**: Контейнеры удаляются после завершения тестов

## Структура тестов

```
tests/FamilyTaskManager.IntegrationTests/
├── Data/
│   ├── BaseRepositoryTestFixture.cs       # Базовый класс с Testcontainers
│   ├── FamilyRepositoryTests.cs           # Тесты для Family агрегата
│   └── TaskTemplateRepositoryTests.cs     # Тесты для TaskTemplate агрегата
└── GlobalUsings.cs
```

## Покрытие тестами

### FamilyRepositoryTests
- ✅ CRUD операции (Add, Update, Delete, GetById)
- ✅ Списки и подсчет (ListAsync, CountAsync)
- ✅ Проверки существования (AnyAsync)
- ✅ Связанные сущности (Members)
- ✅ Batch операции (AddRangeAsync)

### TaskTemplateRepositoryTests
- ✅ CRUD операции
- ✅ Бизнес-логика (Update, Deactivate)
- ✅ Списки и подсчет
- ✅ Проверки существования

## Запуск тестов

### Требования
- **Docker Desktop** должен быть запущен
- При первом запуске скачается образ PostgreSQL (~80 MB)

### Команды

Запустить все интеграционные тесты:
```bash
dotnet test tests/FamilyTaskManager.IntegrationTests
```

Запустить конкретный тестовый класс:
```bash
dotnet test tests/FamilyTaskManager.IntegrationTests --filter "FullyQualifiedName~FamilyRepositoryTests"
```

Запустить с подробным выводом:
```bash
dotnet test tests/FamilyTaskManager.IntegrationTests --logger "console;verbosity=detailed"
```

## Конфигурация

### PostgreSQL контейнер
- **Image**: `postgres:17-alpine`
- **Database**: `familytaskmanager_test`
- **Username**: `postgres`
- **Password**: `Test_password123!`
- **Port**: Динамически назначается Testcontainers

### Время выполнения
- Первый запуск: ~15-20 секунд (старт контейнера + миграции)
- Последующие запуски: ~5-10 секунд

## Добавление новых тестов

### Для нового агрегата

1. Создайте класс, наследующий `BaseRepositoryTestFixture`:

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

2. Каждый тестовый класс автоматически получит свой изолированный контейнер

### Best Practices

- ✅ Используйте `Repository` property вместо поля
- ✅ Не переопределяйте `InitializeAsync` без необходимости
- ✅ Тестируйте бизнес-логику агрегатов, а не только CRUD
- ✅ Проверяйте связанные сущности (навигационные свойства)
- ✅ Используйте `ClearDatabaseAsync()` если нужна очистка между тестами

## Отличия от InMemory

| Аспект | InMemory | Testcontainers + PostgreSQL |
|--------|----------|----------------------------|
| Скорость | Мгновенно | ~5-10 сек |
| Реализм | Низкий | Высокий |
| Constraints | Не поддерживает | Полная поддержка |
| Миграции | Не проверяет | Проверяет |
| Изоляция | Общая память | Отдельные контейнеры |

## Troubleshooting

### Docker не запущен
```
Error: Docker is not running
```
**Решение**: Запустите Docker Desktop

### Порт занят
```
Error: Port already in use
```
**Решение**: Testcontainers автоматически выбирает свободный порт, но если проблема сохраняется - перезапустите Docker

### Медленные тесты
- Первый запуск всегда медленнее (скачивание образа)
- Убедитесь, что Docker Desktop имеет достаточно ресурсов (Settings → Resources)

## Дальнейшее развитие

### Планируется добавить:
- [ ] Тесты для Pet репозитория
- [ ] Тесты для User репозитория  
- [ ] Тесты для ActionHistory репозитория
- [ ] Тесты для Specifications (фильтрация, сортировка)
- [ ] Тесты для транзакций и откатов
- [ ] Тесты для конкурентного доступа
