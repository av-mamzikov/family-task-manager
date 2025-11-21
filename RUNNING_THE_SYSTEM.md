# Запуск системы FamilyTaskManager

Полная инструкция по запуску модульного монолита (Telegram Bot + Quartz Worker в одном процессе).

## Предварительные требования

- ✅ .NET 9.0 SDK
- ✅ PostgreSQL 15+
- ✅ Telegram Bot Token (от @BotFather)

## Архитектура

**Модульный монолит** - единый процесс `FamilyTaskManager.Host`, объединяющий:
- **Bot Module**: Telegram Bot с Long Polling
- **Worker Module**: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, PetMoodCalculator)

## Быстрый старт

### 1. Клонирование и настройка БД

```bash
# Клонировать репозиторий
git clone <repository-url>
cd family-task-manager

# Создать базу данных
psql -U postgres -c "CREATE DATABASE FamilyTaskManager;"
```

**Примечание**: Миграции применяются автоматически при запуске Host.

### 2. Настройка Host

```bash
cd src/FamilyTaskManager.Host

# Настроить токен бота
dotnet user-secrets set "Bot:BotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Bot:BotUsername" "your_bot_username"

# Настроить строку подключения
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=FamilyTaskManager;Username=postgres;Password=YOUR_PASSWORD"
```

### 3. Запуск системы

Откройте **1 терминал** (вместо 3!):

```bash
cd src/FamilyTaskManager.Host
dotnet run
```

Вы должны увидеть:
```
[INF] Starting FamilyTaskManager Host (Modular Monolith)
[INF] Database migration completed
[INF] All modules registered successfully
[INF] Bot Module: Telegram Bot with Long Polling
[INF] Worker Module: Quartz.NET Jobs (TaskInstanceCreator, TaskReminder, PetMoodCalculator)
[INF] Bot Module started: @your_bot_username
[INF] Quartz Scheduler 'FamilyTaskManagerScheduler' started
```

**Готово!** Оба модуля (Bot и Worker) работают в одном процессе.

## Проверка работы

### 1. Проверка Telegram Bot

1. Откройте Telegram
2. Найдите вашего бота по username
3. Отправьте `/start`
4. Создайте семью
5. Создайте питомца
6. Проверьте команды `/family`, `/pet`, `/tasks`, `/stats`

### 2. Проверка Worker

#### Создание периодической задачи

Через SQL или API создайте TaskTemplate:

```sql
-- Получите ID семьи и питомца
SELECT "Id", "Name" FROM "Families";
SELECT "Id", "Name" FROM "Pets";

-- Создайте шаблон задачи
INSERT INTO "TaskTemplates" (
  "Id", 
  "FamilyId", 
  "PetId", 
  "Title", 
  "Points", 
  "Schedule", 
  "CreatedBy", 
  "CreatedAt", 
  "IsActive"
)
VALUES (
  gen_random_uuid(),
  'YOUR_FAMILY_ID',
  'YOUR_PET_ID',
  'Покормить кота',
  10,
  '0 */5 * * * ?',  -- Каждые 5 минут для теста
  'YOUR_USER_ID',
  NOW(),
  true
);
```

#### Проверка создания TaskInstance

Подождите 5 минут и проверьте:

```sql
SELECT 
  ti."Id",
  ti."Title",
  ti."Status",
  ti."DueAt",
  ti."CreatedAt",
  tt."Schedule"
FROM "TaskInstances" ti
JOIN "TaskTemplates" tt ON ti."TemplateId" = tt."Id"
ORDER BY ti."CreatedAt" DESC
LIMIT 5;
```

#### Проверка настроения питомца

Подождите 30 минут (или измените расписание PetMoodCalculatorJob на `0 */2 * * * ?` для теста):

```sql
SELECT 
  "Id",
  "Name",
  "Type",
  "MoodScore",
  "CreatedAt"
FROM "Pets";
```

### 3. Полный сценарий

1. **Создайте семью через бота** (`/start` → "Создать семью")
2. **Создайте питомца** (`/pet` → "Добавить питомца")
3. **Создайте TaskTemplate через SQL** (см. выше)
4. **Подождите срабатывания расписания** (Worker создаст TaskInstance)
5. **Выполните задачу через бота** (`/tasks` → "Взять в работу" → "Выполнить")
6. **Проверьте начисление очков** (`/stats`)
7. **Подождите пересчета настроения** (Worker обновит MoodScore)
8. **Проверьте настроение питомца** (`/pet`)

## Мониторинг

### Единый поток логов

Все логи (Bot и Worker) в одном терминале:

```bash
# Терминал с Host
[INF] Bot Module: Received message from user 123456: /start
[INF] Bot Module: User registered: John Doe (TelegramId: 123456)
[INF] Bot Module: Family created: My Family (Id: abc-123)

[INF] Worker Module: TaskInstanceCreatorJob started at 2025-11-21T12:00:00Z
[INF] Worker Module: Found 1 active task templates
[INF] Worker Module: Creating TaskInstance for template abc-123 (Покормить кота)
[INF] Worker Module: Successfully created TaskInstance xyz-789 from template abc-123
[INF] Worker Module: TaskInstanceCreatorJob completed. Created 1 new task instances

[INF] Worker Module: PetMoodCalculatorJob started at 2025-11-21T12:30:00Z
[INF] Worker Module: Found 1 pets to update mood scores
[INF] Worker Module: Updated mood score for pet abc-456 (Мурзик): 85
[INF] Worker Module: PetMoodCalculatorJob completed. Updated 1 pet mood scores
```

### Фильтрация логов

```bash
# Только Bot логи
dotnet run | grep "Bot Module"

# Только Worker логи
dotnet run | grep "Worker Module"
```

### Проверка состояния Quartz

```sql
-- Список зарегистрированных Jobs
SELECT 
  "SCHED_NAME",
  "JOB_NAME",
  "JOB_GROUP",
  "DESCRIPTION"
FROM "QRTZ_JOB_DETAILS";

-- Список триггеров
SELECT 
  "TRIGGER_NAME",
  "TRIGGER_STATE",
  "NEXT_FIRE_TIME",
  "PREV_FIRE_TIME"
FROM "QRTZ_TRIGGERS";

-- История выполнения (если включен)
SELECT * FROM "QRTZ_FIRED_TRIGGERS" 
ORDER BY "FIRED_TIME" DESC 
LIMIT 10;
```

## Остановка системы

Нажмите `Ctrl+C` в терминале с Host. Система корректно завершит работу:

1. **Bot Module**: Завершит обработку текущих сообщений
2. **Worker Module**: Завершит текущие Jobs (graceful shutdown)
3. **Host**: Корректно закроет все соединения

## Troubleshooting

### Bot не отвечает

**Проблема**: Бот не реагирует на команды

**Решение**:
1. Проверьте токен: `dotnet user-secrets list`
2. Проверьте логи на ошибки подключения
3. Убедитесь, что бот не заблокирован в Telegram

### Worker не создает TaskInstance

**Проблема**: TaskInstance не создаются по расписанию

**Решение**:
1. Проверьте, что TaskTemplate.IsActive = true
2. Проверьте валидность Cron выражения: https://www.freeformatter.com/cron-expression-generator-quartz.html
3. Проверьте логи Worker на ошибки
4. Убедитесь, что предыдущий TaskInstance выполнен

### Настроение питомца не обновляется

**Проблема**: MoodScore остается неизменным

**Решение**:
1. Проверьте, что PetMoodCalculatorJob запускается (логи)
2. Проверьте, что есть задачи с DueAt <= now
3. Проверьте формулу расчета в логах

### Ошибка подключения к БД

**Проблема**: `Npgsql.NpgsqlException: Connection refused`

**Решение**:
1. Проверьте, что PostgreSQL запущен: `pg_isready`
2. Проверьте строку подключения
3. Проверьте права доступа пользователя БД

## Production Deployment

### Docker Compose

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: FamilyTaskManager
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  host:
    build:
      context: .
      dockerfile: src/FamilyTaskManager.Host/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=FamilyTaskManager;Username=postgres;Password=${DB_PASSWORD}"
      Bot__BotToken: ${TELEGRAM_BOT_TOKEN}
      Bot__BotUsername: ${TELEGRAM_BOT_USERNAME}
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped

volumes:
  postgres_data:
```

**Преимущества модульного монолита:**
- 2 контейнера вместо 4 (postgres + host vs postgres + bot + worker + web)
- Проще управление
- Меньше overhead

Запуск:
```bash
export DB_PASSWORD=your_secure_password
export TELEGRAM_BOT_TOKEN=your_bot_token
export TELEGRAM_BOT_USERNAME=your_bot_username

docker-compose up -d
```

## Полезные команды

### Очистка тестовых данных

```sql
-- Удалить все TaskInstances
DELETE FROM "TaskInstances";

-- Удалить все TaskTemplates
DELETE FROM "TaskTemplates";

-- Сбросить настроение питомцев
UPDATE "Pets" SET "MoodScore" = 50;

-- Сбросить очки участников
UPDATE "FamilyMembers" SET "Points" = 0;
```

### Просмотр активных задач

```sql
SELECT 
  ti."Title",
  ti."Status",
  ti."Points",
  ti."DueAt",
  p."Name" as "PetName",
  f."Name" as "FamilyName"
FROM "TaskInstances" ti
JOIN "Pets" p ON ti."PetId" = p."Id"
JOIN "Families" f ON ti."FamilyId" = f."Id"
WHERE ti."Status" IN (0, 1)  -- Active or InProgress
ORDER BY ti."DueAt";
```

### Статистика по семье

```sql
SELECT 
  fm."UserId",
  u."Name",
  fm."Role",
  fm."Points",
  COUNT(ti."Id") as "CompletedTasks"
FROM "FamilyMembers" fm
JOIN "Users" u ON fm."UserId" = u."Id"
LEFT JOIN "TaskInstances" ti ON ti."CompletedBy" = fm."UserId" AND ti."Status" = 2
WHERE fm."FamilyId" = 'YOUR_FAMILY_ID'
GROUP BY fm."UserId", u."Name", fm."Role", fm."Points"
ORDER BY fm."Points" DESC;
```

## Следующие шаги

После успешного запуска системы:

1. ✅ Протестируйте основные сценарии
2. ✅ Создайте несколько семей и питомцев
3. ✅ Настройте периодические задачи
4. 🚧 Интегрируйте Telegram уведомления
5. 🚧 Реализуйте систему invite-кодов
6. 🚧 Добавьте создание задач через бота
