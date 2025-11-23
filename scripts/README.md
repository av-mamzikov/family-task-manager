# Scripts Directory

Скрипты для развертывания, тестирования и мониторинга системы.

## Deployment Scripts

### Private Registry Setup (Рекомендуется для production)

- **`setup-registry.sh`** - Настройка Private Docker Registry на VPS
- **`build-and-push.sh`** - Сборка и отправка образа в registry (Linux/Mac)
- **`build-and-push.ps1`** - Сборка и отправка образа в registry (Windows)
- **`deploy-from-registry.sh`** - Деплой из Private Registry на VPS

📖 **Полная документация:** [docs/PRIVATE_REGISTRY_SETUP.md](../docs/PRIVATE_REGISTRY_SETUP.md)

### Alternative Deployment

- **`deploy-build-on-vps.sh`** - Деплой со сборкой на VPS (без registry)
- **`deploy.sh`** - Базовый скрипт деплоя
- **`server-setup.sh`** - Первоначальная настройка VPS

---

## Database Scripts

Полезные SQL скрипты для тестирования и мониторинга системы.

## test-worker.sql

Комплексный скрипт для тестирования Quartz.NET Worker.

### Что делает скрипт:

1. **Создает тестовые данные**:
   - Test User
   - Test Family
   - Test Pet (Мурзик)
   - 2 TaskTemplate с расписанием каждые 2-3 минуты

2. **Проверяет создание TaskInstance**:
   - Запросы для мониторинга TaskInstances
   - Проверка Quartz Jobs и Triggers

3. **Тестирует расчет настроения**:
   - Создает выполненные и просроченные задачи
   - Проверяет обновление MoodScore

4. **Мониторинг**:
   - Статистика по статусам задач
   - Информация о питомцах
   - Статистика по семьям

### Как использовать:

```bash
# Запустить весь скрипт
psql -U postgres -d FamilyTaskManager -f scripts/test-worker.sql

# Или выполнить отдельные секции в pgAdmin/DBeaver
```

### Порядок тестирования:

1. Запустите Worker:
   ```bash
   cd src/FamilyTaskManager.Worker
   dotnet run
   ```

2. Выполните секцию 1 скрипта (CREATE TEST DATA)

3. Подождите 2-3 минуты

4. Выполните секцию 3 (MONITOR WORKER EXECUTION)

5. Проверьте логи Worker:
   ```
   [INF] TaskInstanceCreatorJob started
   [INF] Found 2 active task templates
   [INF] Creating TaskInstance for template...
   [INF] Successfully created TaskInstance...
   ```

6. Выполните секцию 4 для теста настроения

7. Подождите 30 минут (или измените расписание PetMoodCalculatorJob)

8. Проверьте обновление MoodScore

### Cleanup

Раскомментируйте секцию 5 для удаления тестовых данных.

## Другие полезные скрипты

### Проверка состояния Quartz

```sql
-- Список Jobs
SELECT * FROM "QRTZ_JOB_DETAILS";

-- Список Triggers
SELECT 
    "TRIGGER_NAME",
    "TRIGGER_STATE",
    to_timestamp("NEXT_FIRE_TIME" / 1000) as "NextRun"
FROM "QRTZ_TRIGGERS";

-- История выполнения
SELECT * FROM "QRTZ_FIRED_TRIGGERS" 
ORDER BY "FIRED_TIME" DESC 
LIMIT 20;
```

### Статистика по задачам

```sql
-- Задачи по статусам
SELECT 
    CASE 
        WHEN "Status" = 0 THEN 'Active'
        WHEN "Status" = 1 THEN 'InProgress'
        WHEN "Status" = 2 THEN 'Completed'
    END as "Status",
    COUNT(*) as "Count",
    SUM("Points") as "TotalPoints"
FROM "TaskInstances"
GROUP BY "Status";

-- Просроченные задачи
SELECT 
    ti."Title",
    ti."DueAt",
    NOW() - ti."DueAt" as "Overdue",
    p."Name" as "PetName",
    f."Name" as "FamilyName"
FROM "TaskInstances" ti
JOIN "Pets" p ON ti."PetId" = p."Id"
JOIN "Families" f ON ti."FamilyId" = f."Id"
WHERE ti."Status" != 2 
  AND ti."DueAt" < NOW()
ORDER BY ti."DueAt";
```

### Лидерборд

```sql
SELECT 
    u."Name",
    fm."Role",
    fm."Points",
    COUNT(ti."Id") as "CompletedTasks",
    f."Name" as "FamilyName"
FROM "FamilyMembers" fm
JOIN "Users" u ON fm."UserId" = u."Id"
JOIN "Families" f ON fm."FamilyId" = f."Id"
LEFT JOIN "TaskInstances" ti ON ti."CompletedBy" = fm."UserId" AND ti."Status" = 2
WHERE fm."IsActive" = true
GROUP BY u."Name", fm."Role", fm."Points", f."Name"
ORDER BY fm."Points" DESC;
```

### Настроение питомцев

```sql
SELECT 
    p."Name",
    p."Type",
    p."MoodScore",
    COUNT(ti."Id") as "TotalTasks",
    COUNT(CASE WHEN ti."Status" = 2 THEN 1 END) as "Completed",
    COUNT(CASE WHEN ti."Status" != 2 AND ti."DueAt" < NOW() THEN 1 END) as "Overdue",
    f."Name" as "FamilyName"
FROM "Pets" p
JOIN "Families" f ON p."FamilyId" = f."Id"
LEFT JOIN "TaskInstances" ti ON ti."PetId" = p."Id" AND ti."DueAt" <= NOW()
GROUP BY p."Id", p."Name", p."Type", p."MoodScore", f."Name"
ORDER BY p."MoodScore" DESC;
```

### Очистка данных

```sql
-- Удалить все TaskInstances (осторожно!)
-- DELETE FROM "TaskInstances";

-- Удалить только тестовые данные
DELETE FROM "TaskInstances" 
WHERE "Title" LIKE '%тест%' OR "Title" LIKE '%test%';

-- Сбросить очки
UPDATE "FamilyMembers" SET "Points" = 0;

-- Сбросить настроение питомцев
UPDATE "Pets" SET "MoodScore" = 50;
```

## Troubleshooting

### Worker не создает TaskInstance

```sql
-- Проверить активные шаблоны
SELECT * FROM "TaskTemplates" WHERE "IsActive" = true;

-- Проверить существующие TaskInstance для шаблона
SELECT 
    ti.*,
    CASE 
        WHEN ti."Status" = 0 THEN 'Active'
        WHEN ti."Status" = 1 THEN 'InProgress'
        WHEN ti."Status" = 2 THEN 'Completed'
    END as "StatusText"
FROM "TaskInstances" ti
WHERE ti."TemplateId" = 'YOUR_TEMPLATE_ID'
ORDER BY ti."CreatedAt" DESC;
```

### Quartz не запускается

```sql
-- Проверить таблицы Quartz
SELECT tablename 
FROM pg_tables 
WHERE tablename LIKE 'qrtz_%';

-- Если таблиц нет, они создадутся автоматически при первом запуске Worker
```

### Настроение не обновляется

```sql
-- Проверить задачи питомца
SELECT 
    ti."Title",
    ti."Status",
    ti."Points",
    ti."DueAt",
    ti."CompletedAt",
    CASE 
        WHEN ti."Status" = 2 AND ti."CompletedAt" <= ti."DueAt" THEN 'On time'
        WHEN ti."Status" = 2 AND ti."CompletedAt" > ti."DueAt" THEN 'Late'
        WHEN ti."Status" != 2 AND NOW() > ti."DueAt" THEN 'Overdue'
        ELSE 'Pending'
    END as "TaskStatus"
FROM "TaskInstances" ti
WHERE ti."PetId" = 'YOUR_PET_ID'
  AND ti."DueAt" <= NOW()
ORDER BY ti."DueAt" DESC;
```
