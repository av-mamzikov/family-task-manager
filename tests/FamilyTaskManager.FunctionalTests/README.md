# FamilyTaskManager.FunctionalTests

Функциональные тесты для проверки бизнес-логики и сквозных сценариев с реальной БД.

## 📁 Структура проекта

```
FamilyTaskManager.FunctionalTests/
├── UseCases/              # UseCase тесты с реальной БД
│   ├── Family/           # Тесты управления семьёй
│   ├── Pets/             # Тесты питомцев
│   ├── Tasks/            # Тесты задач
│   ├── Leaderboard/      # Тесты лидерборда
│   └── History/          # Тесты истории действий
│
├── BotFlow/              # Bot Flow сквозные тесты
│   ├── Family/           # Сценарии создания/управления семьёй
│   ├── Pets/             # Сценарии работы с питомцами
│   ├── Tasks/            # Сценарии работы с задачами
│   ├── Leaderboard/      # Сценарии просмотра статистики
│   └── Navigation/       # Навигация и уведомления
│
├── Fixtures/             # Базовые fixtures для тестов (пусто - используем CustomWebApplicationFactory)
│
├── Helpers/              # Вспомогательные классы
│   ├── UpdateFactory.cs      # Создание Telegram Update объектов
│   ├── BotAssertions.cs      # Проверки ответов бота
│   └── TestDataBuilder.cs    # Билдеры тестовых данных
│
├── CustomWebApplicationFactory.cs  # Factory для Bot Flow тестов
└── TestTelegramBotClient.cs       # Mock Telegram Bot Client
```

## 🎯 Типы тестов

### UseCase тесты (`UseCases/`)

**Цель:** Тестирование user stories через MediatR команды/запросы с реальным приложением

**Характеристики:**

- ✅ Реальное приложение через `WebApplicationFactory`
- ✅ Реальная PostgreSQL БД (Testcontainers)
- ✅ Реальный DI контейнер со всеми зависимостями
- ✅ Прямой вызов через `IMediator.Send()` из DI
- ❌ Без HTTP слоя
- ❌ Без Telegram Bot handlers
- ⚡ Средняя скорость (~100-200ms)

**Что тестируют:**

- User stories на уровне UseCase
- Валидацию команд
- Бизнес-правила и инварианты
- Интеграцию с БД (транзакции, constraints)
- Доменную логику
- Реальную конфигурацию приложения

**Пример:**

```csharp
[Fact]
public async Task CreateFamily_WithValidData_ShouldSucceed()
{
    // Arrange - Get services from real DI container
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var command = new CreateFamilyCommand(userId, "Test Family", "UTC");
    
    // Act
    var result = await mediator.Send(command);
    
    // Assert
    result.IsSuccess.ShouldBeTrue();
    var family = await dbContext.Families.FindAsync(result.Value);
    family.ShouldNotBeNull();
}
```

**Базовый класс:** `CustomWebApplicationFactory<Program>`

**Документация:** `TEST_SCENARIOS_USECASE.md`

---

### Bot Flow тесты (`BotFlow/`)

**Цель:** Сквозное тестирование пользовательских сценариев через Telegram Bot

**Характеристики:**

- ✅ Реальная PostgreSQL БД (Testcontainers)
- ✅ Полный стек: Bot handlers → ConversationFlow → UseCase → БД
- ✅ Проверка сообщений, кнопок, навигации
- ✅ Проверка состояния conversation
- ⚡ Медленнее (~200-500ms)

**Что тестируют:**

- Сквозные user journeys
- Корректность сообщений и кнопок
- Навигацию между шагами
- Состояние conversation
- UX и интерфейс бота

**Пример:**

```csharp
[Fact]
public async Task CreateFamily_ThroughBot_ShouldCompleteConversation()
{
    // Arrange
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();
    
    // Act - Step 1: Click "Create Family"
    var callback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    await updateHandler.HandleUpdateAsync(botClient, callback, CancellationToken.None);
    
    // Assert - Check bot response
    var response = botClient.GetLastMessageTo(chatId);
    response.ShouldContainText("Введите название семьи");
    
    // ... продолжение conversation
}
```

**Базовый класс:** `CustomWebApplicationFactory<Program>`

**Документация:** `TEST_SCENARIOS_BOT_FLOW.md`

---

## 🔧 Вспомогательные классы

### `CustomWebApplicationFactory<Program>`

Базовый fixture для **обоих типов** тестов:

- Настройка PostgreSQL контейнера из пула
- Реальное приложение со всеми зависимостями
- Замена `ITelegramBotClient` на `TestTelegramBotClient`
- Доступ к `Services` для получения любых сервисов из DI

### `UpdateFactory`

Фабрика для создания Telegram Update объектов:

- `CreateTextUpdate()` - текстовые сообщения
- `CreateCallbackUpdate()` - нажатия на inline кнопки
- `CreateLocationUpdate()` - отправка геолокации
- `CreateContactUpdate()` - отправка контакта

### `BotAssertions`

Extension методы для проверки ответов бота:

- `ShouldContainText()` - проверка текста сообщения
- `ShouldHaveInlineKeyboard()` - проверка наличия inline клавиатуры
- `ShouldContainButton()` - проверка наличия кнопки
- `GetButton()` - получение кнопки по тексту

### `TestDataBuilder`

Билдеры для создания тестовых данных:

- `CreateUser()` - создание пользователя
- `CreateFamily()` - создание семьи
- `CreateFamilyWithAdmin()` - семья с админом
- `CreateFamilyWithMembers()` - семья с несколькими участниками

---

## 🚀 Запуск тестов

### Все функциональные тесты:

```bash
dotnet test tests/FamilyTaskManager.FunctionalTests
```

### Только UseCase тесты:

```bash
dotnet test tests/FamilyTaskManager.FunctionalTests --filter "FullyQualifiedName~UseCases"
```

### Только Bot Flow тесты:

```bash
dotnet test tests/FamilyTaskManager.FunctionalTests --filter "FullyQualifiedName~BotFlow"
```

### Конкретная категория:

```bash
dotnet test tests/FamilyTaskManager.FunctionalTests --filter "FullyQualifiedName~UseCases.Family"
```

---

## 📊 Сравнение с другими типами тестов

| Тип                     | Проект                      | БД | WebApp | Точка входа     | Скорость | Что тестирует                  |
|-------------------------|-----------------------------|----|--------|-----------------|----------|--------------------------------|
| **Unit**                | `UnitTests/UseCases/`       | ❌  | ❌      | Handler (моки)  | ⚡⚡⚡      | Логика handler'ов изолированно |
| **UseCase Functional**  | `FunctionalTests/UseCases/` | ✅  | ✅      | IMediator       | ⚡⚡       | User stories через UseCase     |
| **Bot Flow Functional** | `FunctionalTests/BotFlow/`  | ✅  | ✅      | Telegram Update | ⚡        | User stories через бота        |
| **Integration**         | `IntegrationTests/`         | ✅  | ❌      | Repository      | ⚡⚡       | Репозитории и инфраструктура   |

---

## 📝 Соглашения

1. **Именование UseCase тестов:**
    - Формат: `TS_UC_XXX_MethodName_ShouldExpectedResult`
    - Пример: `TS_UC_001_CreateFirstFamily_ShouldSucceed`
    - Соответствует сценариям из `TEST_SCENARIOS_USECASE.md`

2. **Именование Bot Flow тестов:**
    - Формат: `TS_BOT_XXX_ScenarioName_ShouldExpectedResult`
    - Пример: `TS_BOT_002_CreateFirstFamily_ShouldCompleteFullConversation`
    - Соответствует сценариям из `TEST_SCENARIOS_BOT_FLOW.md`

3. **Изоляция тестов:**
    - UseCase тесты: каждый тест получает свой scope из `_factory.Services.CreateScope()`
    - Bot Flow тесты: используйте `botClient.Clear()` в начале каждого теста
    - Оба типа используют один PostgreSQL контейнер из пула

4. **Async/Await:**
    - Все тесты должны быть асинхронными
    - Используйте `Task` для всех методов тестов

---

## 🔗 Связанные документы

- [TEST_SCENARIOS_USECASE.md](./TEST_SCENARIOS_USECASE.md) - Сценарии UseCase тестов
- [TEST_SCENARIOS_BOT_FLOW.md](./TEST_SCENARIOS_BOT_FLOW.md) - Сценарии Bot Flow тестов
- [../TestInfrastructure/](../FamilyTaskManager.TestInfrastructure/) - Общая тестовая инфраструктура
