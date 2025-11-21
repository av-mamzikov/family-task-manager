# Repository Tests Architecture

## Структура тестов

Тесты репозиториев построены на базе **generic базового класса** `RepositoryTestsBase<TEntity>`, который содержит все типовые тесты для CRUD операций.

### Иерархия классов

```
BaseRepositoryTestFixture (Testcontainers setup)
    ↓
RepositoryTestsBase<TEntity> (Generic типовые тесты)
    ↓
UserRepositoryTests, PetRepositoryTests, etc. (Специфичные тесты)
```

## RepositoryTestsBase<TEntity>

Базовый generic класс с 9 типовыми тестами:

### Типовые тесты (наследуются автоматически)

1. ✅ **AddAsync_ShouldPersistEntityToDatabase** - добавление сущности
2. ✅ **UpdateAsync_ShouldModifyExistingEntity** - обновление сущности
3. ✅ **DeleteAsync_ShouldRemoveEntityFromDatabase** - удаление сущности
4. ✅ **GetByIdAsync_WithNonExistentId_ShouldReturnNull** - получение несуществующей сущности
5. ✅ **ListAsync_ShouldReturnAllEntities** - получение списка всех сущностей
6. ✅ **CountAsync_ShouldReturnCorrectCount** - подсчет количества
7. ✅ **AnyAsync_WithExistingData_ShouldReturnTrue** - проверка наличия данных
8. ✅ **AnyAsync_WithEmptyDatabase_ShouldReturnFalse** - проверка пустой БД
9. ✅ **AddRangeAsync_ShouldPersistMultipleEntities** - добавление нескольких сущностей

### Абстрактные методы (должны быть реализованы)

```csharp
protected abstract TEntity CreateTestEntity(string uniqueSuffix = "");
protected abstract TEntity CreateSecondTestEntity(string uniqueSuffix = "");
protected abstract void ModifyEntity(TEntity entity);
protected abstract void AssertEntityWasModified(TEntity entity);
```

## Как добавить тесты для нового репозитория

### Шаг 1: Создать класс-наследник

```csharp
using FamilyTaskManager.Core.YourAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class YourEntityRepositoryTests : RepositoryTestsBase<YourEntity>
{
  // Реализовать абстрактные методы
}
```

### Шаг 2: Реализовать абстрактные методы

```csharp
protected override YourEntity CreateTestEntity(string uniqueSuffix = "")
{
  return new YourEntity($"Test{uniqueSuffix}", ...);
}

protected override YourEntity CreateSecondTestEntity(string uniqueSuffix = "")
{
  return new YourEntity($"Another{uniqueSuffix}", ...);
}

protected override void ModifyEntity(YourEntity entity)
{
  entity.UpdateSomething("New Value");
}

protected override void AssertEntityWasModified(YourEntity entity)
{
  entity.Something.ShouldBe("New Value");
}
```

### Шаг 3: Добавить специфичные тесты (опционально)

```csharp
[Fact]
public async Task YourSpecificBusinessLogic_ShouldWork()
{
  // Arrange
  var entity = CreateTestEntity();
  
  // Act
  entity.DoSomethingSpecific();
  await Repository.AddAsync(entity);
  await DbContext.SaveChangesAsync();
  
  // Assert
  var retrieved = await Repository.GetByIdAsync(entity.Id);
  retrieved.ShouldNotBeNull();
  // Ваши специфичные проверки
}
```

## Примеры реализации

### UserRepositoryTests

```csharp
public class UserRepositoryTests : RepositoryTestsBase<User>
{
  protected override User CreateTestEntity(string uniqueSuffix = "")
  {
    return new User(123456789 + long.Parse(uniqueSuffix == "" ? "0" : uniqueSuffix), 
                    $"Test User{uniqueSuffix}");
  }

  protected override User CreateSecondTestEntity(string uniqueSuffix = "")
  {
    return new User(987654321 + long.Parse(uniqueSuffix == "" ? "0" : uniqueSuffix), 
                    $"Another User{uniqueSuffix}");
  }

  protected override void ModifyEntity(User entity)
  {
    entity.UpdateName("Updated Name");
  }

  protected override void AssertEntityWasModified(User entity)
  {
    entity.Name.ShouldBe("Updated Name");
  }

  // Специфичный тест для User
  [Fact]
  public async Task User_ShouldHaveCorrectTelegramId()
  {
    var user = new User(555555555, "Telegram User");
    await Repository.AddAsync(user);
    await DbContext.SaveChangesAsync();

    var retrieved = await Repository.GetByIdAsync(user.Id);
    retrieved.ShouldNotBeNull();
    retrieved.TelegramId.ShouldBe(555555555);
  }
}
```

### PetRepositoryTests

```csharp
public class PetRepositoryTests : RepositoryTestsBase<Pet>
{
  protected override Pet CreateTestEntity(string uniqueSuffix = "")
  {
    return new Pet(Guid.NewGuid(), PetType.Cat, $"Fluffy{uniqueSuffix}");
  }

  protected override Pet CreateSecondTestEntity(string uniqueSuffix = "")
  {
    return new Pet(Guid.NewGuid(), PetType.Dog, $"Buddy{uniqueSuffix}");
  }

  protected override void ModifyEntity(Pet entity)
  {
    entity.UpdateName("Modified Pet Name");
    entity.UpdateMoodScore(75);
  }

  protected override void AssertEntityWasModified(Pet entity)
  {
    entity.Name.ShouldBe("Modified Pet Name");
    entity.MoodScore.ShouldBe(75);
  }

  // Специфичные тесты для Pet
  [Fact]
  public async Task Pet_ShouldHaveDefaultMoodScore()
  {
    var pet = new Pet(Guid.NewGuid(), PetType.Hamster, "Hammy");
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    var retrieved = await Repository.GetByIdAsync(pet.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(50);
  }
}
```

## Преимущества подхода

### ✅ Нет дублирования кода
- Все типовые тесты написаны один раз в базовом классе
- Наследники получают их автоматически

### ✅ Консистентность
- Все репозитории тестируются одинаково
- Единый подход к тестированию

### ✅ Легко расширять
- Добавление нового репозитория = 4 метода + специфичные тесты
- Изменение базовых тестов применяется ко всем репозиториям

### ✅ Читаемость
- Каждый тестовый класс фокусируется только на специфике своей сущности
- Базовые тесты скрыты в базовом классе

## Текущее покрытие

| Агрегат | Типовые тесты | Специфичные тесты | Всего |
|---------|---------------|-------------------|-------|
| **User** | 9 | 1 | 10 |
| **Pet** | 9 | 2 | 11 |
| Family | - | 11 | 11 (старый подход) |
| TaskTemplate | - | 8 | 8 (старый подход) |

## Следующие шаги

Рекомендуется:
1. Мигрировать `FamilyRepositoryTests` и `TaskTemplateRepositoryTests` на новый подход
2. Добавить тесты для оставшихся агрегатов:
   - TaskInstance
   - ActionHistory

## Запуск тестов

```bash
# Все тесты репозиториев
dotnet test tests/FamilyTaskManager.IntegrationTests

# Только User тесты
dotnet test --filter "FullyQualifiedName~UserRepositoryTests"

# Только Pet тесты
dotnet test --filter "FullyQualifiedName~PetRepositoryTests"

# User и Pet вместе
dotnet test --filter "FullyQualifiedName~UserRepositoryTests|FullyQualifiedName~PetRepositoryTests"
```
