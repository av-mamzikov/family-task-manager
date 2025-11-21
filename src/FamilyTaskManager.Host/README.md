# FamilyTaskManager.Host

**Модульный монолит** - единый процесс, объединяющий Bot и Worker модули.

## Архитектура

```
FamilyTaskManager.Host/
├── Modules/
│   ├── Bot/                    # Telegram Bot модуль
│   │   ├── Configuration/
│   │   ├── Handlers/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── BotModuleExtensions.cs
│   └── Worker/                 # Quartz Worker модуль
│       ├── Jobs/
│       └── WorkerModuleExtensions.cs
├── Program.cs                  # Единая точка входа
└── appsettings.json
```

## Преимущества модульного монолита

### ✅ Простота
- Один процесс для запуска
- Одна база данных
- Общий DbContext pool
- Единая конфигурация

### ✅ Производительность
- Нет сетевых вызовов между модулями
- Общий memory cache
- Меньше overhead

### ✅ Разработка
- Проще отладка
- Быстрее старт
- Меньше инфраструктуры

### ✅ Модульность
- Четкое разделение ответственности
- Легко выделить в микросервисы позже
- Независимые модули

## Модули

### Bot Module
**Ответственность**: Взаимодействие с пользователями через Telegram

**Компоненты**:
- `TelegramBotService` - Long Polling
- `SessionManager` - Управление сессиями
- `UpdateHandler` - Обработка обновлений
- Command Handlers - Обработка команд

**Регистрация**:
```csharp
builder.Services.AddBotModule(builder.Configuration);
```

### Worker Module
**Ответственность**: Фоновые задачи по расписанию

**Компоненты**:
- `TaskInstanceCreatorJob` - Создание TaskInstance (каждую минуту)
- `TaskReminderJob` - Напоминания о задачах (каждые 15 минут)
- `PetMoodCalculatorJob` - Пересчет настроения (каждые 30 минут)

**Регистрация**:
```csharp
builder.Services.AddWorkerModule(builder.Configuration);
```

## Быстрый старт

### 1. Настройка

```bash
cd src/FamilyTaskManager.Host

# Настроить токен бота
dotnet user-secrets set "Bot:BotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Bot:BotUsername" "your_bot_username"

# Настроить БД
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=YOUR_PASSWORD"
```

### 2. Запуск

```bash
dotnet run
```

### 3. Проверка

Вы должны увидеть:
```
[INF] Starting FamilyTaskManager Host (Modular Monolith)
[INF] Database migration completed
[INF] All modules registered successfully
[INF] Bot Module: Telegram Bot with Long Polling
[INF] Worker Module: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, PetMoodCalculator)
[INF] Bot @your_bot_username is running
[INF] Quartz Scheduler 'FamilyTaskManagerScheduler' started
```

## Конфигурация

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FamilyTaskManager;..."
  },
  "Bot": {
    "BotToken": "YOUR_BOT_TOKEN",
    "BotUsername": "your_bot_username"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Quartz": "Information"
    }
  }
}
```

## Масштабирование

### Вертикальное (текущее)
```
Один процесс Host:
- Bot Module
- Worker Module
- Shared DbContext
```

**Подходит для**: 0-1000 пользователей

### Горизонтальное (будущее)
```
Несколько инстансов Host:
- Host Instance 1 (Bot + Worker)
- Host Instance 2 (Bot + Worker)
- Host Instance 3 (Bot + Worker)
- Load Balancer
- Shared PostgreSQL
```

**Подходит для**: 1000-10000 пользователей

### Микросервисы (при необходимости)
```
Отдельные сервисы:
- Bot Service (3 инстанса)
- Worker Service (1 инстанс)
- API Service (2 инстанса)
- Message Bus (RabbitMQ/Kafka)
```

**Подходит для**: 10000+ пользователей

## Миграция на микросервисы

Когда нужно:
- ✅ > 10000 активных пользователей
- ✅ Разные команды на Bot и Worker
- ✅ Независимое масштабирование критично
- ✅ Разные SLA для модулей

Как:
1. Вынести модули в отдельные проекты
2. Добавить Message Bus
3. Настроить API Gateway
4. Настроить Service Discovery

## Мониторинг

### Логи
```bash
# Все логи
tail -f logs/host.log

# Только Bot
tail -f logs/host.log | grep "Bot"

# Только Worker
tail -f logs/host.log | grep "Worker"
```

### Метрики
- Количество обработанных сообщений (Bot)
- Количество созданных TaskInstance (Worker)
- Количество пересчитанных питомцев (Worker)
- Время выполнения Jobs (Worker)

## Troubleshooting

### Bot не запускается
```
Проверить:
1. Bot:BotToken настроен
2. Токен валидный
3. БД доступна
```

### Worker не создает задачи
```
Проверить:
1. Quartz таблицы созданы (QRTZ_*)
2. TaskTemplate.IsActive = true
3. Cron выражение валидное
```

### Оба модуля не работают
```
Проверить:
1. PostgreSQL запущен
2. ConnectionString корректный
3. Миграции применены
```

## Сравнение с микросервисами

| Критерий | Модульный монолит | Микросервисы |
|----------|-------------------|--------------|
| Простота | ✅ Высокая | ❌ Низкая |
| Производительность | ✅ Высокая | ⚠️ Средняя |
| Масштабируемость | ⚠️ Ограничена | ✅ Неограничена |
| Отказоустойчивость | ❌ Низкая | ✅ Высокая |
| Overhead | ✅ Низкий | ❌ Высокий |
| Подходит для MVP | ✅ Да | ❌ Нет |

## Следующие шаги

1. ✅ Модульный монолит работает
2. ⏳ Добавить Telegram уведомления
3. ⏳ Реализовать invite codes
4. ⏳ Добавить создание задач через бота
5. ⏳ Мониторинг и метрики
6. ⏳ При необходимости - миграция на микросервисы

## Заключение

**Модульный монолит - оптимальный выбор для MVP:**
- ✅ Проще разработка и поддержка
- ✅ Быстрее запуск
- ✅ Меньше инфраструктуры
- ✅ Легко мигрировать на микросервисы позже

**Когда переходить на микросервисы:**
- Только при реальной необходимости
- Не раньше 10000+ пользователей
- Когда модульный монолит становится узким местом
