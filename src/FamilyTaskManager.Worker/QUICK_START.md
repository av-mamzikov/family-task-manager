# Quick Start - FamilyTaskManager Worker

## Предварительные требования

1. **.NET 9.0 SDK** или выше
2. **PostgreSQL 15+** (запущен и доступен)
3. **База данных FamilyTaskManager** (должна быть создана и мигрирована)

## Шаг 1: Настройка базы данных

Убедитесь, что PostgreSQL запущен и база данных создана:

```bash
# Создание базы данных (если еще не создана)
psql -U postgres -c "CREATE DATABASE FamilyTaskManager;"
```

Примените миграции EF Core:

```bash
cd ../FamilyTaskManager.Infrastructure
dotnet ef database update --startup-project ../FamilyTaskManager.Web
```

## Шаг 2: Настройка строки подключения

### Вариант 1: User Secrets (рекомендуется для разработки)

```bash
cd ../FamilyTaskManager.Worker
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=YOUR_PASSWORD"
```

### Вариант 2: appsettings.Development.json

Создайте или отредактируйте `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

## Шаг 3: Установка Quartz таблиц

При первом запуске Quartz.NET автоматически создаст необходимые таблицы в PostgreSQL:
- QRTZ_JOB_DETAILS
- QRTZ_TRIGGERS
- QRTZ_CALENDARS
- и другие...

## Шаг 4: Запуск Worker

```bash
cd src/FamilyTaskManager.Worker
dotnet run
```

Вы должны увидеть логи:

```
[INF] Starting FamilyTaskManager Worker
[INF] Database migration completed
[INF] Quartz Scheduler 'FamilyTaskManagerScheduler' started
[INF] TaskInstanceCreatorJob started at 2025-11-21T12:00:00Z
```

## Проверка работы

### 1. Создайте TaskTemplate через API или напрямую в БД

```sql
INSERT INTO "TaskTemplates" ("Id", "FamilyId", "PetId", "Title", "Points", "Schedule", "CreatedBy", "CreatedAt", "IsActive")
VALUES (
  gen_random_uuid(),
  'YOUR_FAMILY_ID',
  'YOUR_PET_ID',
  'Покормить кота',
  10,
  '0 0 9 * * ?',  -- Каждый день в 9:00
  'YOUR_USER_ID',
  NOW(),
  true
);
```

### 2. Проверьте создание TaskInstance

Через минуту после срабатывания расписания проверьте таблицу `TaskInstances`:

```sql
SELECT * FROM "TaskInstances" 
WHERE "TemplateId" = 'YOUR_TEMPLATE_ID' 
ORDER BY "CreatedAt" DESC;
```

### 3. Проверьте пересчет настроения питомца

Через 30 минут проверьте обновление `MoodScore`:

```sql
SELECT "Id", "Name", "MoodScore" FROM "Pets";
```

## Расписание Jobs

| Job | Расписание | Описание |
|-----|-----------|----------|
| TaskInstanceCreatorJob | Каждую минуту | Создает TaskInstance из TaskTemplate |
| TaskReminderJob | Каждые 15 минут | Отправляет напоминания о задачах |
| PetMoodCalculatorJob | Каждые 30 минут | Пересчитывает настроение питомцев |

## Остановка Worker

Нажмите `Ctrl+C` для graceful shutdown. Quartz завершит текущие задачи перед остановкой.

## Troubleshooting

### Ошибка подключения к БД

```
Npgsql.NpgsqlException: Connection refused
```

**Решение**: Проверьте, что PostgreSQL запущен и доступен на порту 5432.

### Ошибка миграции

```
The EF Core tools version '9.0.0' is older than that of the runtime '9.0.8'
```

**Решение**: Обновите EF Core tools:
```bash
dotnet tool update --global dotnet-ef
```

### Job не запускается

**Решение**: Проверьте логи Quartz. Убедитесь, что таблицы QRTZ_* созданы в БД.

```sql
SELECT tablename FROM pg_tables WHERE tablename LIKE 'qrtz_%';
```

### Cron выражение невалидно

**Решение**: Используйте формат Quartz Cron (6 или 7 полей):
- `0 0 9 * * ?` - каждый день в 9:00
- `0 */15 * * * ?` - каждые 15 минут

Проверить выражение: https://www.freeformatter.com/cron-expression-generator-quartz.html

## Production Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FamilyTaskManager.Worker.dll"]
```

### Systemd Service

```ini
[Unit]
Description=FamilyTaskManager Worker
After=network.target postgresql.service

[Service]
Type=notify
WorkingDirectory=/opt/familytaskmanager/worker
ExecStart=/usr/bin/dotnet FamilyTaskManager.Worker.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

## Мониторинг

Логи пишутся в консоль через Serilog. Для production рекомендуется:

1. Добавить Serilog.Sinks.File
2. Настроить Application Insights
3. Использовать Seq или ELK для централизованных логов

```csharp
Log.Logger = new LoggerConfiguration()
  .WriteTo.Console()
  .WriteTo.File("logs/worker-.txt", rollingInterval: RollingInterval.Day)
  .CreateLogger();
```

## Следующие шаги

После запуска Worker:
1. Интегрируйте Telegram уведомления в TaskReminderJob
2. Настройте Domain Event Handlers
3. Добавьте систему invite-кодов в Telegram Bot
4. Реализуйте создание задач через бота
