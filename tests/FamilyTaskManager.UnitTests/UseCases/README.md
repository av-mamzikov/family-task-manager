# Use Cases Unit Tests

## Обзор

Unit тесты для всех Use Cases проекта. Используют NSubstitute для мокирования зависимостей и Shouldly для assertions.

## Покрытие тестами

### ✅ Users (3 теста)
- `RegisterUserHandlerTests`
  - ✓ Handle_NewUser_CreatesUserAndReturnsId
  - ✓ Handle_ExistingUser_ReturnsExistingUserId
  - ✓ Handle_ExistingUserWithDifferentName_UpdatesNameAndReturnsId

### ✅ Families (8 тестов)
- `CreateFamilyHandlerTests`
  - ✓ Handle_ValidCommand_CreatesFamilyWithAdminMember
  - ✓ Handle_NonExistentUser_ReturnsNotFound
  - ✓ Handle_DefaultTimezone_UsesUTC

- `JoinFamilyHandlerTests`
  - ✓ Handle_ValidCommand_AddsMemberToFamily
  - ✓ Handle_NonExistentUser_ReturnsNotFound
  - ✓ Handle_NonExistentFamily_ReturnsNotFound
  - ✓ Handle_UserAlreadyMember_ReturnsError

### ✅ Tasks (11 тестов)
- `CreateTaskHandlerTests`
  - ✓ Handle_ValidCommand_CreatesTask
  - ✓ Handle_NonExistentPet_ReturnsNotFound
  - ✓ Handle_PetFromDifferentFamily_ReturnsError
  - ✓ Handle_InvalidTitle_ReturnsInvalid (2 cases)
  - ✓ Handle_InvalidPoints_ReturnsInvalid (3 cases)

- `CompleteTaskHandlerTests`
  - ✓ Handle_ValidCommand_CompletesTaskAndAddsPoints
  - ✓ Handle_NonExistentTask_ReturnsNotFound
  - ✓ Handle_AlreadyCompletedTask_ReturnsError
  - ✓ Handle_UserNotInFamily_ReturnsError

### ✅ Pets (8 тестов)
- `CreatePetHandlerTests`
  - ✓ Handle_ValidCommand_CreatesPet
  - ✓ Handle_NonExistentFamily_ReturnsNotFound
  - ✓ Handle_InvalidName_ReturnsInvalid (3 cases)
  - ✓ Handle_DifferentPetTypes_CreatesCorrectType (3 cases)

### ✅ Statistics (4 теста)
- `GetLeaderboardHandlerTests`
  - ✓ Handle_ValidQuery_ReturnsLeaderboardSortedByPoints
  - ✓ Handle_NonExistentFamily_ReturnsNotFound
  - ✓ Handle_LeaderboardDisabled_ReturnsError
  - ✓ Handle_OnlyActiveMembers_ExcludesInactiveMembers

## Итого: 34 теста

## Запуск тестов

```bash
# Все Use Cases тесты
dotnet test tests\FamilyTaskManager.UnitTests\FamilyTaskManager.UnitTests.csproj --filter "FullyQualifiedName~UseCases"

# Конкретная категория
dotnet test --filter "FullyQualifiedName~UseCases.Users"
dotnet test --filter "FullyQualifiedName~UseCases.Families"
dotnet test --filter "FullyQualifiedName~UseCases.Tasks"
dotnet test --filter "FullyQualifiedName~UseCases.Pets"
dotnet test --filter "FullyQualifiedName~UseCases.Statistics"
```

## Паттерны тестирования

### Arrange-Act-Assert
Все тесты следуют паттерну AAA:
```csharp
[Fact]
public async Task Handle_ValidCommand_CreatesEntity()
{
    // Arrange
    var command = new CreateCommand(...);
    _repository.Setup(...);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.ShouldBeTrue();
    // ... additional assertions
}
```

### Мокирование репозиториев
Используется NSubstitute для создания моков:
```csharp
_repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
    .Returns(entity);
```

### Захват созданных сущностей
Для проверки ID созданных сущностей:
```csharp
Entity? capturedEntity = null;
await _repository.AddAsync(Arg.Do<Entity>(e => capturedEntity = e), Arg.Any<CancellationToken>());

// Act
var result = await _handler.Handle(command, CancellationToken.None);

// Assert
capturedEntity.ShouldNotBeNull();
result.Value.ShouldBe(capturedEntity.Id);
```

### Проверка результатов
Используется Shouldly для читаемых assertions:
```csharp
result.IsSuccess.ShouldBeTrue();
result.Status.ShouldBe(ResultStatus.NotFound);
result.Value.ShouldNotBe(Guid.Empty);
entity.Name.ShouldBe("Expected Name");
```

## Тестовые сценарии

### Happy Path
- Валидные данные создают сущности
- ID генерируются корректно
- Связи между сущностями устанавливаются

### Валидация
- Невалидные данные возвращают Invalid
- Проверка длины строк
- Проверка диапазонов чисел

### Бизнес-правила
- Несуществующие сущности возвращают NotFound
- Дублирование предотвращается
- Права доступа проверяются

### Edge Cases
- Пустые строки
- Граничные значения
- Неактивные сущности

## Следующие шаги

- [ ] Добавить тесты для TaskTemplate Use Cases
- [ ] Добавить тесты для GetActionHistory
- [ ] Добавить тесты для UpdatePetName
- [ ] Добавить integration тесты с реальной БД
- [ ] Добавить тесты для Domain Event Handlers

---

**Статус:** ✅ 34/34 теста проходят  
**Покрытие:** Основные Use Cases покрыты  
**Дата:** 21 ноября 2025
