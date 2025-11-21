# Use Cases Documentation

## Обзор

Use Cases реализуют бизнес-логику приложения, следуя принципам Clean Architecture и CQRS (Command Query Responsibility Segregation).

## Структура

```
FamilyTaskManager.UseCases/
├── Users/                    # Управление пользователями
│   ├── RegisterUser.cs       # Регистрация/обновление пользователя
│   └── Specifications/
│       ├── GetUserByTelegramIdSpec.cs
│       └── GetUsersByIdsSpec.cs
├── Families/                 # Управление семьями
│   ├── CreateFamily.cs       # Создание семьи
│   ├── JoinFamily.cs         # Присоединение к семье
│   ├── GetUserFamilies.cs    # Получение списка семей пользователя
│   └── Specifications/
│       ├── GetFamiliesByUserIdSpec.cs
│       └── GetFamilyWithMembersSpec.cs
├── Tasks/                    # Управление задачами
│   ├── CreateTask.cs         # Создание разовой задачи
│   ├── TakeTask.cs           # Взятие задачи в работу
│   ├── CompleteTask.cs       # Выполнение задачи
│   ├── GetActiveTasks.cs     # Получение активных задач
│   ├── CreateTaskTemplate.cs # Создание шаблона периодической задачи
│   ├── UpdateTaskTemplate.cs # Обновление шаблона
│   ├── DeactivateTaskTemplate.cs # Деактивация шаблона
│   └── Specifications/
│       └── GetActiveTasksByFamilySpec.cs
├── Pets/                     # Управление питомцами
│   ├── CreatePet.cs          # Создание питомца
│   ├── GetPets.cs            # Получение списка питомцев
│   ├── UpdatePetName.cs      # Изменение имени питомца
│   └── Specifications/
│       └── GetPetsByFamilySpec.cs
└── Statistics/               # Статистика
    ├── GetLeaderboard.cs     # Получение лидерборда
    ├── GetActionHistory.cs   # Получение истории действий
    └── Specifications/
        └── GetActionHistorySpec.cs
```

## Основные Use Cases

### 1. Пользователи

#### RegisterUserCommand
**Назначение:** Регистрация нового пользователя или обновление имени существующего.

**Параметры:**
- `TelegramId` (long) - ID пользователя в Telegram
- `Name` (string) - Имя пользователя

**Возвращает:** `Result<Guid>` - ID пользователя

**Логика:**
1. Проверяет, существует ли пользователь с данным TelegramId
2. Если существует - обновляет имя (если изменилось)
3. Если не существует - создаёт нового пользователя

---

### 2. Семьи

#### CreateFamilyCommand
**Назначение:** Создание новой семьи.

**Параметры:**
- `UserId` (Guid) - ID создателя семьи
- `Name` (string) - Название семьи
- `Timezone` (string, default: "UTC") - Часовой пояс
- `LeaderboardEnabled` (bool, default: true) - Включен ли лидерборд

**Возвращает:** `Result<Guid>` - ID созданной семьи

**Логика:**
1. Проверяет существование пользователя
2. Создаёт семью
3. Добавляет создателя как Админа

#### JoinFamilyCommand
**Назначение:** Присоединение пользователя к семье.

**Параметры:**
- `UserId` (Guid) - ID пользователя
- `FamilyId` (Guid) - ID семьи
- `Role` (FamilyRole) - Роль в семье

**Возвращает:** `Result<Guid>` - ID созданного FamilyMember

**Логика:**
1. Проверяет существование пользователя и семьи
2. Проверяет, не является ли пользователь уже членом семьи
3. Добавляет пользователя в семью с указанной ролью

#### GetUserFamiliesQuery
**Назначение:** Получение списка семей пользователя.

**Параметры:**
- `UserId` (Guid) - ID пользователя

**Возвращает:** `Result<List<FamilyDto>>`

**DTO:**
```csharp
public record FamilyDto(
  Guid Id, 
  string Name, 
  string Timezone, 
  bool LeaderboardEnabled, 
  FamilyRole UserRole, 
  int UserPoints);
```

---

### 3. Задачи

#### CreateTaskCommand
**Назначение:** Создание разовой задачи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи
- `PetId` (Guid) - ID питомца
- `Title` (string, 3-100 символов) - Название задачи
- `Points` (int, 1-100) - Количество очков
- `DueAt` (DateTime) - Срок выполнения
- `CreatedBy` (Guid) - ID создателя

**Возвращает:** `Result<Guid>` - ID созданной задачи

**Валидация:**
- Питомец должен существовать и принадлежать семье
- Название: 3-100 символов
- Очки: 1-100

#### TakeTaskCommand
**Назначение:** Взятие задачи в работу.

**Параметры:**
- `TaskId` (Guid) - ID задачи
- `UserId` (Guid) - ID пользователя

**Возвращает:** `Result`

**Логика:**
1. Проверяет, что задача существует и имеет статус Active
2. Переводит задачу в статус InProgress

#### CompleteTaskCommand
**Назначение:** Выполнение задачи с начислением очков.

**Параметры:**
- `TaskId` (Guid) - ID задачи
- `UserId` (Guid) - ID пользователя

**Возвращает:** `Result`

**Логика:**
1. Проверяет существование задачи
2. Проверяет, что пользователь является членом семьи
3. Помечает задачу как выполненную
4. Начисляет очки пользователю
5. Генерирует доменное событие TaskCompletedEvent

#### GetActiveTasksQuery
**Назначение:** Получение списка активных задач семьи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи

**Возвращает:** `Result<List<TaskDto>>`

**DTO:**
```csharp
public record TaskDto(
  Guid Id, 
  string Title, 
  int Points, 
  TaskStatus Status, 
  DateTime DueAt,
  Guid PetId,
  string PetName);
```

#### CreateTaskTemplateCommand
**Назначение:** Создание шаблона периодической задачи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи
- `PetId` (Guid) - ID питомца
- `Title` (string, 3-100 символов) - Название
- `Points` (int, 1-100) - Очки
- `Schedule` (string) - Quartz Cron Expression
- `CreatedBy` (Guid) - ID создателя

**Возвращает:** `Result<Guid>` - ID шаблона

**Примеры Schedule:**
- `0 0 9 * * ?` - ежедневно в 9:00
- `0 0 9 */5 * ?` - каждые 5 дней в 9:00
- `0 0 20 * * ?` - ежедневно в 20:00

#### UpdateTaskTemplateCommand
**Назначение:** Обновление шаблона задачи.

**Параметры:**
- `TemplateId` (Guid) - ID шаблона
- `Title` (string?, optional) - Новое название
- `Points` (int?, optional) - Новые очки
- `Schedule` (string?, optional) - Новое расписание

**Возвращает:** `Result`

**Примечание:** Изменения применяются только к будущим TaskInstance.

#### DeactivateTaskTemplateCommand
**Назначение:** Деактивация шаблона (прекращение создания новых задач).

**Параметры:**
- `TemplateId` (Guid) - ID шаблона

**Возвращает:** `Result`

---

### 4. Питомцы

#### CreatePetCommand
**Назначение:** Создание питомца.

**Параметры:**
- `FamilyId` (Guid) - ID семьи
- `Type` (PetType) - Тип питомца (Cat/Dog/Hamster)
- `Name` (string, 2-50 символов) - Имя питомца

**Возвращает:** `Result<Guid>` - ID питомца

**TODO:** Автоматическое создание рекомендованных TaskTemplate для типа питомца.

#### GetPetsQuery
**Назначение:** Получение списка питомцев семьи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи

**Возвращает:** `Result<List<PetDto>>`

**DTO:**
```csharp
public record PetDto(
  Guid Id, 
  string Name, 
  PetType Type, 
  int MoodScore);
```

#### UpdatePetNameCommand
**Назначение:** Изменение имени питомца.

**Параметры:**
- `PetId` (Guid) - ID питомца
- `NewName` (string, 2-50 символов) - Новое имя

**Возвращает:** `Result`

---

### 5. Статистика

#### GetLeaderboardQuery
**Назначение:** Получение лидерборда семьи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи

**Возвращает:** `Result<List<LeaderboardEntryDto>>`

**DTO:**
```csharp
public record LeaderboardEntryDto(
  Guid UserId, 
  string UserName, 
  int Points, 
  FamilyRole Role);
```

**Логика:**
1. Проверяет, что лидерборд включен в настройках семьи
2. Возвращает активных участников, отсортированных по очкам

#### GetActionHistoryQuery
**Назначение:** Получение истории действий семьи.

**Параметры:**
- `FamilyId` (Guid) - ID семьи
- `UserId` (Guid?, optional) - Фильтр по пользователю
- `DaysBack` (int?, optional) - Количество дней назад
- `Limit` (int, default: 100) - Максимальное количество записей

**Возвращает:** `Result<List<ActionHistoryDto>>`

**DTO:**
```csharp
public record ActionHistoryDto(
  Guid Id,
  string ActionType,
  string Description,
  DateTime CreatedAt,
  Guid UserId,
  string UserName);
```

---

## Использование

### Пример вызова команды через Mediator

```csharp
// Регистрация пользователя
var command = new RegisterUserCommand(123456789, "John Doe");
var result = await mediator.Send(command);

if (result.IsSuccess)
{
    var userId = result.Value;
    // Работа с userId
}
```

### Пример вызова запроса

```csharp
// Получение активных задач
var query = new GetActiveTasksQuery(familyId);
var result = await mediator.Send(query);

if (result.IsSuccess)
{
    var tasks = result.Value;
    foreach (var task in tasks)
    {
        Console.WriteLine($"{task.Title}: {task.Points} points");
    }
}
```

---

## Паттерны и принципы

### CQRS
- **Commands** - изменяют состояние системы, возвращают Result или Result<T>
- **Queries** - только читают данные, не изменяют состояние

### Specification Pattern
Используется для инкапсуляции логики запросов к БД:
- Переиспользуемые запросы
- Тестируемость
- Разделение ответственности

### Result Pattern
Все Use Cases возвращают `Result` или `Result<T>`:
- `Result.Success()` / `Result<T>.Success(value)`
- `Result.Error(message)`
- `Result.NotFound(message)`
- `Result.Invalid(validationError)`

---

## Следующие шаги

1. **Добавить валидацию через FluentValidation** для более сложных правил
2. **Реализовать Domain Event Handlers** для обработки событий (начисление очков, обновление настроения питомца)
3. **Добавить Unit Tests** для каждого Use Case
4. **Реализовать автоматическое создание TaskTemplate** при создании питомца
5. **Добавить Use Cases для управления приглашениями** (генерация invite codes)

---

## Зависимости

- `Ardalis.Result` - Result pattern
- `Ardalis.Specification` - Specification pattern
- `Mediator.Abstractions` - CQRS pattern
- `FamilyTaskManager.Core` - Domain model

---

**Версия:** 1.0  
**Дата:** 21 ноября 2025
