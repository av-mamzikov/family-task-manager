# FamilyTaskManager.UseCases

## Обзор

Application Layer проекта, содержащий бизнес-логику в виде Use Cases. Реализован по принципам Clean Architecture и CQRS.

## Реализованные Use Cases

### 👤 Users (Пользователи)
- **RegisterUserCommand** - Регистрация/обновление пользователя по TelegramId

### 👨‍👩‍👧‍👦 Families (Семьи)
- **CreateFamilyCommand** - Создание новой семьи
- **JoinFamilyCommand** - Присоединение к семье
- **GetUserFamiliesQuery** - Получение списка семей пользователя

### ✅ Tasks (Задачи)
- **CreateTaskCommand** - Создание разовой задачи
- **TakeTaskCommand** - Взятие задачи в работу
- **CompleteTaskCommand** - Выполнение задачи с начислением очков
- **GetActiveTasksQuery** - Получение активных задач семьи
- **CreateTaskTemplateCommand** - Создание шаблона периодической задачи
- **UpdateTaskTemplateCommand** - Обновление шаблона
- **DeactivateTaskTemplateCommand** - Деактивация шаблона

### 🐾 Pets (Питомцы)
- **CreatePetCommand** - Создание питомца
- **GetPetsQuery** - Получение списка питомцев
- **UpdatePetNameCommand** - Изменение имени питомца

### 📊 Statistics (Статистика)
- **GetLeaderboardQuery** - Получение лидерборда семьи
- **GetActionHistoryQuery** - Получение истории действий

## Архитектура

### CQRS Pattern
- **Commands** - изменяют состояние, возвращают `Result` или `Result<T>`
- **Queries** - только читают данные, не изменяют состояние

### Specification Pattern
Используется для инкапсуляции логики запросов к БД.

### Result Pattern
Все Use Cases возвращают типизированные результаты:
- `Result.Success()` / `Result<T>.Success(value)`
- `Result.Error(message)`
- `Result.NotFound(message)`
- `Result.Invalid(validationError)`

## Использование

```csharp
// Пример команды
var command = new RegisterUserCommand(telegramId: 123456789, name: "John Doe");
var result = await mediator.Send(command);

if (result.IsSuccess)
{
    var userId = result.Value;
    // ...
}

// Пример запроса
var query = new GetActiveTasksQuery(familyId);
var result = await mediator.Send(query);

if (result.IsSuccess)
{
    var tasks = result.Value;
    // ...
}
```

## Зависимости

- `Ardalis.Result` - Result pattern
- `Ardalis.Specification` - Specification pattern  
- `Mediator.Abstractions` - CQRS pattern
- `FamilyTaskManager.Core` - Domain model

## Документация

Подробная документация: [docs/USE_CASES.md](../../docs/USE_CASES.md)

## Следующие шаги

- [ ] Domain Event Handlers для обработки событий
- [ ] FluentValidation для валидации команд
- [ ] Unit Tests для всех Use Cases
- [ ] Автоматическое создание TaskTemplate при создании питомца
- [ ] Use Cases для управления приглашениями (invite codes)
