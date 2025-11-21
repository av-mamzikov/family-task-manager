# Руководство по тестированию Telegram бота

## Обзор

Функциональные тесты для Telegram бота позволяют проверить работу бота без реального подключения к Telegram API. Тесты симулируют взаимодействие пользователя с ботом и проверяют корректность обработки команд, callback queries и сохранения данных в базе.

## Структура тестов

```
tests/FamilyTaskManager.FunctionalTests/Bot/
├── FakeTelegramBotClient.cs      # Mock Telegram Bot API
├── TestUpdateFactory.cs          # Фабрика тестовых данных
├── BotFunctionalTestBase.cs     # Базовый класс для тестов
├── BotCommandTests.cs            # Тесты команд (/start, /help и т.д.)
├── BotCallbackQueryTests.cs     # Тесты кнопок (inline keyboard)
├── BotIntegrationTests.cs       # Комплексные сценарии
└── README.md                     # Подробная документация
```

## Быстрый старт

### 1. Создание простого теста

```csharp
public class MyBotTests : BotFunctionalTestBase
{
    [Fact]
    public async Task My_First_Test()
    {
        // Arrange
        const long chatId = 123456789;
        const long userId = 987654321;
        
        // Act - отправить команду /start
        await SendCommandAsync(chatId, userId, "/start");
        
        // Assert - проверить ответ бота
        var message = FakeBotClient.GetLastSentMessage();
        message.ShouldNotBeNull();
    }
}
```

### 2. Запуск тестов

```powershell
# Все тесты бота
dotnet test --filter "FullyQualifiedName~FamilyTaskManager.FunctionalTests.Bot"

# Конкретный класс
dotnet test --filter "FullyQualifiedName~BotCommandTests"
```

## Основные сценарии

### Тестирование команды

```csharp
[Fact]
public async Task Start_Command_Works()
{
    await SendCommandAsync(chatId, userId, "/start");
    
    FakeBotClient.SentMessages.ShouldNotBeEmpty();
}
```

### Тестирование кнопки

```csharp
[Fact]
public async Task Create_Family_Button_Works()
{
    await SendCallbackQueryAsync(chatId, userId, "create_family");
    
    FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();
}
```

### Тестирование диалога

```csharp
[Fact]
public async Task Family_Creation_Flow()
{
    // 1. Нажать кнопку
    await SendCallbackQueryAsync(chatId, userId, "create_family");
    
    // 2. Ввести название
    await SendTextMessageAsync(chatId, userId, "Моя семья");
    
    // 3. Проверить в БД
    using var scope = ServiceProvider!.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var family = await db.Families.FirstOrDefaultAsync();
    
    family.ShouldNotBeNull();
    family!.Name.ShouldBe("Моя семья");
}
```

## Доступные методы

| Метод | Описание |
|-------|----------|
| `SendCommandAsync(chatId, userId, "/command")` | Отправить команду |
| `SendTextMessageAsync(chatId, userId, "text")` | Отправить текст |
| `SendCallbackQueryAsync(chatId, userId, "data")` | Нажать кнопку |
| `FakeBotClient.GetLastSentMessage()` | Последнее сообщение бота |
| `FakeBotClient.GetLastEditedMessage()` | Последнее отредактированное сообщение |
| `FakeBotClient.AnsweredCallbacks` | Список обработанных callbacks |

## Проверки (Assertions)

```csharp
// Проверка сообщений
FakeBotClient.SentMessages.ShouldNotBeEmpty();
FakeBotClient.SentMessages.Count.ShouldBe(3);

var msg = FakeBotClient.GetLastSentMessage();
msg.ShouldNotBeNull();
msg!.Text.ShouldContain("Привет");

// Проверка callbacks
FakeBotClient.AnsweredCallbacks.ShouldNotBeEmpty();

// Проверка БД
using var scope = ServiceProvider!.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var families = await db.Families.ToListAsync();
families.Count.ShouldBe(1);
```

## Преимущества

✅ **Быстро** - нет реальных HTTP-запросов  
✅ **Надежно** - полная изоляция тестов  
✅ **Реалистично** - тестируется вся цепочка обработки  
✅ **Удобно** - простой API для написания тестов  

## Что тестировать

### ✅ Обязательно
- Основные команды (`/start`, `/help`, `/family`, `/tasks`)
- Создание сущностей (семья, питомец, задача)
- Обработка callback queries
- Валидация входных данных
- Сохранение в базу данных

### ⚠️ Желательно
- Граничные случаи (пустые данные, слишком длинные строки)
- Обработка ошибок
- Проверка прав доступа
- Сложные сценарии (несколько пользователей)

### ❌ Не нужно
- Тестировать Telegram API (это mock)
- Тестировать инфраструктуру (Entity Framework, NSubstitute)
- Дублировать unit-тесты

## Примеры из кода

См. готовые примеры:
- `BotCommandTests.cs` - тесты команд
- `BotCallbackQueryTests.cs` - тесты кнопок
- `BotIntegrationTests.cs` - комплексные сценарии

## Отладка тестов

### Visual Studio
1. Поставьте breakpoint в тесте
2. Запустите тест в режиме отладки (Debug Test)
3. Шагайте по коду

### Логирование
Тесты используют консольное логирование. Вывод доступен в Test Explorer.

## Troubleshooting

### Тест падает с ошибкой "ServiceProvider is not initialized"
**Решение**: Убедитесь, что тест наследуется от `BotFunctionalTestBase` и вызывается `InitializeAsync()` автоматически через `IAsyncLifetime`.

### Тест не видит данные в БД
**Решение**: Используйте новый scope для доступа к БД:
```csharp
using var scope = ServiceProvider!.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

### FakeBotClient не перехватывает вызовы
**Решение**: Проверьте, что используется правильная перегрузка метода Telegram API. Mock настроен для основных методов.

## Дополнительная информация

Подробная документация: `tests/FamilyTaskManager.FunctionalTests/Bot/README.md`

## Контрибьюция

При добавлении новых функций бота:
1. Добавьте тесты для новых команд
2. Добавьте тесты для новых callback queries
3. Добавьте интеграционный тест для полного сценария
4. Обновите документацию
