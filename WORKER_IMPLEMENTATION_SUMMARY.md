# Worker Implementation Summary

## Обзор

Реализован полнофункциональный Quartz.NET Worker для автоматизации периодических задач в системе FamilyTaskManager.

## Что реализовано

### 1. Use Cases для Worker

#### Tasks
- **CreateTaskInstanceFromTemplateCommand** - создание TaskInstance из TaskTemplate
- **GetActiveTaskTemplatesQuery** - получение всех активных шаблонов задач
- **GetTasksDueForReminderQuery** - получение задач, требующих напоминания

#### Pets
- **CalculatePetMoodScoreCommand** - расчет и обновление настроения питомца
- **GetAllPetsQuery** - получение всех питомцев

#### Users
- **GetUserByIdQuery** - получение пользователя по ID (для уведомлений)

#### Specifications
- **TaskInstancesByTemplateSpec** - поиск TaskInstance по TemplateId
- **ActiveTaskTemplatesSpec** - фильтр активных TaskTemplate
- **TasksDueForReminderSpec** - фильтр задач для напоминаний
- **TasksByPetSpec** - фильтр задач по питомцу

### 2. Quartz Jobs

#### TaskInstanceCreatorJob
```csharp
// Расписание: каждую минуту (0 * * * * ?)
// Функционал:
// - Получает все активные TaskTemplate
// - Парсит Quartz Cron выражение каждого шаблона
// - Проверяет, не пора ли создать новый TaskInstance
// - Создает TaskInstance только если предыдущий выполнен
// - Логирует все операции
```

**Инвариант**: Для одного TaskTemplate может существовать не более одного активного TaskInstance.

#### TaskReminderJob
```csharp
// Расписание: каждые 15 минут (0 */15 * * * ?)
// Функционал:
// - Получает задачи, срок которых наступит в течение часа
// - Группирует по семьям
// - Получает активных участников каждой семьи
// - Отправляет напоминания (пока только логирование)
```

**Примечание**: Полная интеграция с Telegram Bot для отправки уведомлений будет добавлена позже.

#### PetMoodCalculatorJob
```csharp
// Расписание: каждые 30 минут (0 */30 * * * ?)
// Функционал:
// - Получает всех питомцев
// - Для каждого питомца:
//   - Получает все задачи, срок которых уже наступил
//   - Рассчитывает effectiveSum по формуле из ТЗ:
//     * Выполнено вовремя: полные очки
//     * Выполнено с опозданием: 50% очков
//     * Не выполнено и просрочено: отрицательный вклад
//   - Обновляет Pet.MoodScore
```

**Формула настроения** (из ТЗ раздел 2.2):
```
baseMood = 100 * (effectiveSum / maxPoints)
moodScore = clamp(0, 100, round(baseMood))
```

### 3. Infrastructure

#### Пакеты
- `Quartz 3.13.1` - планировщик задач
- `Quartz.Extensions.Hosting 3.13.1` - интеграция с .NET Hosting
- `Quartz.Serialization.Json 3.13.1` - JSON сериализация
- `Cronos 0.8.4` - парсинг Cron выражений (не используется, заменен на Quartz CronExpression)

#### Конфигурация
- PostgreSQL для персистентности Jobs
- Serilog для структурированного логирования
- Mediator для CQRS
- EF Core для доступа к данным

#### Program.cs
- Настройка DI контейнера
- Регистрация Quartz с PostgreSQL persistence
- Регистрация всех Jobs с расписаниями
- Автоматическое применение миграций при старте
- Graceful shutdown

### 4. Документация

- **README.md** - полное описание Worker, архитектура, Use Cases
- **QUICK_START.md** - пошаговая инструкция по запуску
- Обновлен главный README.md проекта

## Архитектура

```
┌─────────────────────────────────────────────────────────┐
│                    Quartz Scheduler                      │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────┐│
│  │TaskInstanceJob │  │TaskReminderJob │  │PetMoodJob  ││
│  │  Every minute  │  │  Every 15 min  │  │ Every 30min││
│  └────────┬───────┘  └────────┬───────┘  └──────┬─────┘│
└───────────┼──────────────────┼──────────────────┼──────┘
            │                  │                  │
            ▼                  ▼                  ▼
     ┌──────────────────────────────────────────────────┐
     │                   Mediator                        │
     └──────────────────────┬───────────────────────────┘
                            │
            ┌───────────────┼───────────────┐
            ▼               ▼               ▼
    ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
    │ Task UseCases│ │ Pet UseCases │ │ User UseCases│
    └──────┬───────┘ └──────┬───────┘ └──────┬───────┘
           │                │                │
           └────────────────┼────────────────┘
                            ▼
                   ┌─────────────────┐
                   │  Infrastructure │
                   │   (Repository)  │
                   └────────┬────────┘
                            ▼
                   ┌─────────────────┐
                   │   PostgreSQL    │
                   └─────────────────┘
```

## Соответствие ТЗ

### Раздел 2.3 - Задачи (Периодические)

✅ **Реализовано**:
- Создание TaskInstance по расписанию Quartz Cron
- Инвариант: не более одного активного TaskInstance на TaskTemplate
- Автоматическое создание нового экземпляра после выполнения предыдущего

### Раздел 2.2 - Питомцы (Механика настроения)

✅ **Реализовано**:
- Расчет настроения на лету по всем связанным задачам
- Учет просроченных задач с отрицательным вкладом
- Формула с effectivePoints и clamp(0, 100)
- Периодический пересчет каждые 30 минут

### Раздел 2.7 - Уведомления

⏳ **Частично реализовано**:
- Job для напоминаний за 1 час до задачи ✅
- Получение списка участников семьи ✅
- Отправка в Telegram ⏳ (требует интеграции с Bot)

### Раздел 6.2 - Quartz.NET Worker

✅ **Реализовано**:
- Проверка расписания каждую минуту
- Создание TaskInstance для периодических задач
- Отправка напоминаний (логика готова, Telegram интеграция pending)
- Персистентность через PostgreSQL

## Тестирование

### Ручное тестирование

1. **Создать TaskTemplate**:
```sql
INSERT INTO "TaskTemplates" ("Id", "FamilyId", "PetId", "Title", "Points", "Schedule", "CreatedBy", "CreatedAt", "IsActive")
VALUES (gen_random_uuid(), 'family-id', 'pet-id', 'Test Task', 10, '0 */5 * * * ?', 'user-id', NOW(), true);
```

2. **Запустить Worker**:
```bash
cd src/FamilyTaskManager.Worker
dotnet run
```

3. **Проверить логи**:
```
[INF] TaskInstanceCreatorJob started
[INF] Found 1 active task templates
[INF] Creating TaskInstance for template...
[INF] Successfully created TaskInstance...
```

4. **Проверить БД**:
```sql
SELECT * FROM "TaskInstances" WHERE "TemplateId" = 'your-template-id';
```

### Unit тесты (TODO)

- [ ] TaskInstanceCreatorJob tests
- [ ] TaskReminderJob tests
- [ ] PetMoodCalculatorJob tests
- [ ] CalculatePetMoodScoreHandler tests

## Известные ограничения

1. **Telegram уведомления**: TaskReminderJob пока только логирует, не отправляет реальные сообщения
2. **Timezone**: Все расписания в UTC, поддержка Family.Timezone будет добавлена
3. **Частота напоминаний**: Нет проверки "не чаще 1 раза в час" (требует хранения lastReminderSent)
4. **Horizontal scaling**: Quartz настроен для single instance, для кластера нужна доп. конфигурация

## Следующие шаги

### Приоритет 1
1. Интегрировать Telegram уведомления в TaskReminderJob
2. Добавить хранение lastReminderSent для TaskInstance
3. Реализовать поддержку Family.Timezone

### Приоритет 2
4. Добавить Domain Event Handlers (TaskCompleted → обновление настроения)
5. Создать Use Case для автоматического создания TaskTemplate при создании Pet
6. Добавить метрики и health checks

### Приоритет 3
7. Unit и Integration тесты для всех Jobs
8. Настроить Quartz для horizontal scaling
9. Добавить dashboard для мониторинга Jobs

## Файлы проекта

```
src/FamilyTaskManager.Worker/
├── Jobs/
│   ├── TaskInstanceCreatorJob.cs      (95 строк)
│   ├── TaskReminderJob.cs             (89 строк)
│   └── PetMoodCalculatorJob.cs        (75 строк)
├── Program.cs                         (106 строк)
├── appsettings.json                   (конфигурация Quartz)
├── README.md                          (полная документация)
└── QUICK_START.md                     (инструкция по запуску)

src/FamilyTaskManager.UseCases/
├── Tasks/
│   ├── CreateTaskInstanceFromTemplate.cs
│   ├── GetActiveTaskTemplates.cs
│   ├── GetTasksDueForReminder.cs
│   └── Specifications/
│       ├── TaskInstancesByTemplateSpec.cs
│       ├── ActiveTaskTemplatesSpec.cs
│       ├── TasksDueForReminderSpec.cs
│       └── TasksByPetSpec.cs
├── Pets/
│   ├── CalculatePetMoodScore.cs
│   └── GetAllPets.cs
└── Users/
    └── GetUserById.cs
```

## Заключение

Worker полностью функционален и готов к использованию. Основной функционал из ТЗ реализован:
- ✅ Автоматическое создание TaskInstance по расписанию
- ✅ Пересчет настроения питомцев
- ✅ Логика напоминаний (требует интеграции с Telegram)

Следующий логический шаг - интеграция уведомлений с Telegram Bot и реализация системы invite-кодов.
