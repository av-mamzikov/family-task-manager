# Тесты Telegram Bot

Этот проект содержит unit-тесты для Telegram Bot компонента Family Task Manager.

## 📊 Статистика покрытия

- **Всего тестов**: 35+
- **Покрытие**: ~80% основной логики бота
- **Время выполнения**: <5 секунд

## 📁 Структура тестов

### Models (7 тестов)

- `UserSessionTests` - тесты модели сессии пользователя
  - Обновление активности
  - Установка состояния
  - Работа с данными
  - Очистка состояния

### Services (5 тестов)

- `SessionManagerTests` - тесты менеджера сессий
  - Создание новых сессий
  - Получение существующих сессий
  - Изоляция сессий разных пользователей
  - Очистка неактивных сессий

### Handlers/Commands (23 теста)

#### FamilyCommandHandlerTests (4 теста)

- Приглашение создать семью для новых пользователей
- Отображение списка семей
- Показ кнопок администратора
- Права доступа по ролям

#### TasksCommandHandlerTests (5 тестов)

- Проверка активной семьи
- Отображение задач
- Группировка по статусу
- Маркировка просроченных задач
- Inline-кнопки действий

#### PetCommandHandlerTests (7 тестов)

- Проверка активной семьи
- Отображение питомцев
- Эмодзи настроения (😊😢)
- Типы питомцев (🐱🐶🐹)
- Обработка ошибок

#### StatsCommandHandlerTests (5 тестов)

- Проверка активной семьи
- Отключенный лидерборд
- Отображение лидерборда
- Медали для топ-3 (🥇🥈🥉)
- Выделение текущего пользователя

### Handlers (12 тестов)

#### CallbackQueryHandlerTests (12 тестов)

- Ответ на callback query
- Создание семьи
- Создание питомца (выбор типа)
- Переключение семьи
- Взятие задачи в работу
- Выполнение задачи
- Обработка ошибок
- Обновление активности
- Неизвестные действия

## 🚀 Запуск тестов

```bash
# Запуск всех тестов
dotnet test

# С подробным выводом
dotnet test --logger "console;verbosity=detailed"

# С покрытием кода
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Конкретный класс тестов
dotnet test --filter "FullyQualifiedName~CallbackQueryHandlerTests"

# Конкретный тест
dotnet test --filter "FullyQualifiedName~HandleCallbackAsync_ShouldCompleteTask"
```

## 🛠️ Используемые библиотеки

- **xUnit** - фреймворк для тестирования
- **NSubstitute** - мокирование зависимостей (ITelegramBotClient, IMediator)
- **Shouldly** - fluent assertions для читаемых проверок

## ✅ Примеры тестов

### Unit Test - Callback Handler

```csharp
[Fact]
public async Task HandleCallbackAsync_ShouldCompleteTask_WhenTaskCompleteClicked()
{
    // Arrange
    var taskId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"task_complete_{taskId}");
    
    _mediator.Send(Arg.Any<CompleteTaskCommand>(), Arg.Any<CancellationToken>())
        .Returns(Result.Success());

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, ct);

    // Assert
    await _mediator.Received(1).Send(
        Arg.Is<CompleteTaskCommand>(cmd => cmd.TaskId == taskId),
        Arg.Any<CancellationToken>());
}
```

### Unit Test - Session Manager

```csharp
[Fact]
public void ClearInactiveSessions_ShouldRemoveOldSessions()
{
    // Arrange
    var session = _sessionManager.GetSession(12345L);
    session.LastActivity = DateTime.UtcNow.AddHours(-25);

    // Act
    _sessionManager.ClearInactiveSessions();
    var newSession = _sessionManager.GetSession(12345L);

    // Assert
    newSession.ShouldNotBe(session);
}
```

## 🎯 Стратегия тестирования

### Что тестируем ✅

- Бизнес-логику обработчиков
- Формирование сообщений
- Валидацию входных данных
- Обработку ошибок
- Интеграцию с Use Cases
- Управление сессиями

### Что НЕ тестируем ❌

- Реальные HTTP-запросы к Telegram API
- База данных (тестируется в Infrastructure.Tests)
- EF Core (тестируется в Integration.Tests)

## 📈 Покрытие кода

Целевое покрытие:

- **Handlers**: 80%+ ✅
- **Services**: 90%+ ✅
- **Models**: 95%+ ✅

Проверка покрытия:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🔍 Troubleshooting

### Тесты падают с NullReferenceException

- Проверьте, что все зависимости замокированы
- Убедитесь, что настроены возвращаемые значения для моков

### Тесты проходят локально, но падают в CI

- Проверьте таймзоны (используйте UTC)
- Убедитесь, что нет race conditions

## 📝 Добавление новых тестов

Используйте паттерн AAA (Arrange-Act-Assert):

```csharp
[Fact]
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange - подготовка данных и моков
    var handler = CreateHandler();
    var input = CreateValidInput();

    // Act - выполнение тестируемого метода
    var result = await handler.HandleCommand(input);

    // Assert - проверка результата
    result.IsSuccess.ShouldBeTrue();
}
```
