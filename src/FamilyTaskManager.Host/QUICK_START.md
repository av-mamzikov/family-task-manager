# Quick Start - FamilyTaskManager.Host (Модульный монолит)

## Что это?

**Единый процесс**, объединяющий Telegram Bot и Quartz Worker в одном приложении.

## Предварительные требования

- .NET 9.0 SDK
- PostgreSQL 15+
- Telegram Bot Token (от @BotFather)

## Запуск за 3 шага

### 1. Настройка

```bash
cd src/FamilyTaskManager.Host

# Bot токен
dotnet user-secrets set "Bot:BotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Bot:BotUsername" "your_bot_username"

# База данных
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
[INF] Worker Module: Quartz.NET Jobs
[INF] Bot Module started: @your_bot_username
[INF] Quartz Scheduler 'FamilyTaskManagerScheduler' started
```

## Что работает?

### ✅ Bot Module

- Telegram Bot с Long Polling
- Все команды: /start, /family, /tasks, /pet, /stats
- Persistent Menu
- Inline Keyboards
- Session Management

### ✅ Worker Module

- TaskInstanceCreatorJob (каждую минуту)
- TaskReminderJob (каждые 15 минут)
- PetMoodCalculatorJob (каждые 30 минут)

## Остановка

Нажмите `Ctrl+C` - оба модуля корректно остановятся.

## Troubleshooting

### Ошибка: Bot configuration is missing

```bash
# Проверьте user secrets
dotnet user-secrets list

# Должно быть:
# Bot:BotToken = YOUR_TOKEN
# Bot:BotUsername = your_username
```

### Ошибка: Cannot connect to PostgreSQL

```bash
# Проверьте PostgreSQL
pg_isready

# Проверьте connection string
dotnet user-secrets list | grep ConnectionStrings
```

### Ошибка: Quartz tables not found

```bash
# Quartz создаст таблицы автоматически при первом запуске
# Проверьте логи на ошибки
```

## Следующие шаги

1. ✅ Host запущен
2. ⏳ Откройте бота в Telegram
3. ⏳ Отправьте /start
4. ⏳ Создайте семью и питомца
5. ⏳ Проверьте, что Worker создает задачи

## Сравнение с раздельными сервисами

| Что          | Раздельно (Bot + Worker) | Монолит (Host) |
|--------------|--------------------------|----------------|
| Процессов    | 2                        | 1              |
| Портов       | 0 (оба не слушают)       | 0              |
| Конфигураций | 2 файла                  | 1 файл         |
| Запуск       | 2 команды                | 1 команда      |
| Остановка    | 2x Ctrl+C                | 1x Ctrl+C      |
| Логи         | 2 потока                 | 1 поток        |
| Отладка      | Сложнее                  | Проще          |

## Production

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "FamilyTaskManager.Host.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: FamilyTaskManager
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

  host:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=FamilyTaskManager;Username=postgres;Password=${DB_PASSWORD}"
      Bot__BotToken: ${BOT_TOKEN}
      Bot__BotUsername: ${BOT_USERNAME}
    depends_on:
      - postgres
    restart: unless-stopped

volumes:
  postgres_data:
```

Запуск:

```bash
docker-compose up -d
```

## Преимущества

✅ **Один процесс** - проще управление
✅ **Общий DbContext** - меньше подключений к БД
✅ **Единая конфигурация** - проще настройка
✅ **Проще отладка** - все в одном месте
✅ **Модульность сохранена** - легко выделить позже

Готово! Система работает как единое целое.
