# Функциональные тесты Telegram бота

Этот каталог содержит функциональные тесты для Telegram бота Family Task Manager.

## Архитектура тестирования

### Компоненты

1. **FakeTelegramBotClient** - Mock-реализация `ITelegramBotClient` на основе NSubstitute
   - Перехватывает все вызовы Telegram Bot API
   - Сохраняет историю отправленных сообщений, отредактированных сообщений и ответов на callback queries
   - Позволяет проверять взаимодействие бота с пользователем

2. **TestUpdateFactory** - Фабрика для создания тестовых Update объектов
   - Создает текстовые сообщения
   - Создает callback queries (нажатия кнопок)
   - Создает команды
   - Упрощает генерацию тестовых данных

3. **BotFunctionalTestBase** - Базовый класс для всех тестов бота
   - Настраивает DI контейнер с in-memory базой данных
   - Инициализирует все необходимые сервисы (Mediator, handlers, repositories)
   - Предоставляет helper-методы для симуляции действий пользователя
   - Автоматически очищает ресурсы после каждого теста

## Примеры использования

### Тестирование команд

```csharp
[Fact]
public async Task Start_Command_Should_Send_Welcome_Message()
{
    // Act - симулируем отправку команды /start
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    
    // Assert - проверяем, что бот отправил приветственное сообщение
    FakeBotClient.SentMessages.ShouldNotBeEmpty();
    var lastMessage = FakeBotClient.GetLastSentMessage();
    lastMessage.ShouldNotBeNull();
    lastMessage!.Text.ShouldNotBeNull();
    lastMessage.Text!.ShouldContain("Добро пожаловать");
}
```

### Тестирование callback queries (кнопок)

```csharp
[Fact]
public async Task Create_Family_Callback_Should_Prompt_For_Family_Name()
{
    // Act - симулируем нажатие кнопки "Создать семью"
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    
    // Assert - проверяем, что callback был обработан
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
    
    // Проверяем, что сообщение было отредактировано
    var lastEdit = FakeBotClient.GetLastEditedMessage();
    lastEdit.HasValue.ShouldBeTrue();
    lastEdit!.Value.Text.ShouldContain("название семьи");
}
```

### Тестирование полного сценария

```csharp
[Fact]
public async Task Complete_Family_Creation_Flow()
{
    // 1. Пользователь запускает бота
    await SendCommandAsync(TestChatId, TestUserId, "/start");
    
    // 2. Пользователь нажимает "Создать семью"
    await SendCallbackQueryAsync(TestChatId, TestUserId, "create_family");
    
    // 3. Пользователь вводит название семьи
    await SendTextMessageAsync(TestChatId, TestUserId, "Моя семья");
    
    // 4. Проверяем, что семья создана в базе данных
    using var scope = ServiceProvider!.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var families = await dbContext.Families.ToListAsync();
    
    families.ShouldNotBeEmpty();
    families.First().Name.ShouldBe("Моя семья");
}
```

## Доступные helper-методы

### SendCommandAsync
Отправляет команду боту (например, `/start`, `/help`).

```csharp
await SendCommandAsync(chatId, userId, "/start");
```

### SendTextMessageAsync
Отправляет текстовое сообщение боту.

```csharp
await SendTextMessageAsync(chatId, userId, "Привет, бот!");
```

### SendCallbackQueryAsync
Симулирует нажатие inline-кнопки.

```csharp
await SendCallbackQueryAsync(chatId, userId, "create_family", messageId: 1);
```

### SendUpdateAsync
Отправляет произвольный Update объект (для продвинутых сценариев).

```csharp
var update = TestUpdateFactory.CreateTextMessage(chatId, userId, "Текст");
await SendUpdateAsync(update);
```

## Проверка результатов

### Проверка отправленных сообщений

```csharp
// Получить последнее отправленное сообщение
var lastMessage = FakeBotClient.GetLastSentMessage();

// Получить все отправленные сообщения
var allMessages = FakeBotClient.SentMessages;

// Проверить количество сообщений
FakeBotClient.SentMessages.Count.ShouldBe(3);
```

### Проверка отредактированных сообщений

```csharp
// Получить последнее отредактированное сообщение
var lastEdit = FakeBotClient.GetLastEditedMessage();

// Получить все отредактированные сообщения
var allEdits = FakeBotClient.EditedMessages;
```

### Проверка ответов на callback queries

```csharp
// Проверить, что callback был обработан
FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();

// Проверить конкретный callback ID
FakeBotClient.WasCallbackAnswered("callback_id_123").ShouldBeTrue();
```

### Проверка данных в базе

```csharp
using var scope = ServiceProvider!.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// Проверить созданные сущности
var families = await dbContext.Families.ToListAsync();
families.ShouldNotBeEmpty();

var tasks = await dbContext.Tasks.Where(t => t.FamilyId == familyId).ToListAsync();
tasks.Count.ShouldBe(5);
```

## Запуск тестов

### Через командную строку

```powershell
# Запустить все тесты бота
dotnet test --filter "FullyQualifiedName~FamilyTaskManager.FunctionalTests.Bot"

# Запустить конкретный тестовый класс
dotnet test --filter "FullyQualifiedName~BotCommandTests"

# Запустить конкретный тест
dotnet test --filter "FullyQualifiedName~Start_Command_Should_Send_Welcome_Message"
```

### Через Visual Studio
1. Откройте Test Explorer (Test → Test Explorer)
2. Найдите тесты в разделе `FamilyTaskManager.FunctionalTests.Bot`
3. Запустите нужные тесты

## Преимущества подхода

✅ **Полная изоляция** - каждый тест использует свою in-memory базу данных  
✅ **Быстрое выполнение** - нет реальных HTTP-запросов к Telegram API  
✅ **Детальная проверка** - можно проверить все аспекты взаимодействия  
✅ **Легкость отладки** - все выполняется локально  
✅ **Реалистичность** - тестируется вся цепочка: Update → Handler → MediatR → Database  

## Ограничения

⚠️ **Не тестируется реальный Telegram API** - используется mock  
⚠️ **Не тестируется сетевое взаимодействие** - все локально  
⚠️ **Требуется поддержка** - при изменении API нужно обновлять mock  

## Рекомендации

1. **Тестируйте бизнес-логику, а не инфраструктуру** - фокусируйтесь на поведении бота
2. **Используйте осмысленные имена тестов** - они должны описывать сценарий
3. **Проверяйте состояние базы данных** - убедитесь, что данные сохранены корректно
4. **Тестируйте граничные случаи** - невалидные данные, отсутствующие сущности и т.д.
5. **Группируйте связанные тесты** - используйте отдельные классы для разных функций

## Дальнейшее развитие

- [ ] Добавить тесты для сложных сценариев (создание задач, управление питомцами)
- [ ] Добавить тесты для обработки ошибок
- [ ] Добавить тесты для проверки прав доступа (роли в семье)
- [ ] Добавить тесты для периодических задач
- [ ] Добавить тесты для системы очков и достижений
