# Следующие шаги после реализации Worker

## ✅ Что сделано

1. **Quartz.NET Worker** - полностью реализован и готов к использованию
2. **Use Cases для Worker** - все необходимые команды и запросы
3. **Jobs** - TaskInstanceCreator, TaskReminder, PetMoodCalculator
4. **Документация** - полная документация и инструкции
5. **Скрипты** - автоматизация запуска и тестирования
6. **Telegram уведомления** - отправка уведомлений о задачах и настроении питомца
7. **Система invite-кодов** - возможность приглашения участников в семью
8. **Создание задач через бота**
9. **Автоматическое создание TaskTemplate**

## Что осталось сделать

### 6. Unit и Integration тесты (3-5 дней)

**Что делать**:

```csharp
// 1. Unit тесты для Jobs
public class TaskInstanceCreatorJobTests
{
    [Fact]
    public async Task Execute_CreatesTaskInstance_WhenScheduleMatches()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<TaskInstanceCreatorJob>>();
        var job = new TaskInstanceCreatorJob(mediator, logger);
        
        // Act
        await job.Execute(context);
        
        // Assert
        await mediator.Received(1).Send(Arg.Any<CreateTaskInstanceFromTemplateCommand>());
    }
}

// 2. Integration тесты для Worker
public class WorkerIntegrationTests : IClassFixture<PostgreSqlContainer>
{
    [Fact]
    public async Task TaskInstanceCreatorJob_CreatesInstance_EndToEnd()
    {
        // Arrange: создать TaskTemplate в тестовой БД
        // Act: запустить Job
        // Assert: проверить создание TaskInstance
    }
}
```

**Файлы для создания**:
- `tests/FamilyTaskManager.WorkerTests/Jobs/TaskInstanceCreatorJobTests.cs`
- `tests/FamilyTaskManager.WorkerTests/Jobs/PetMoodCalculatorJobTests.cs`
- `tests/FamilyTaskManager.IntegrationTests/Worker/WorkerIntegrationTests.cs`

## 🎯 Приоритет 3: Улучшения

### 7. Docker и Docker Compose (1 день)

```dockerfile
# Dockerfile для Worker
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/FamilyTaskManager.Worker/", "FamilyTaskManager.Worker/"]
RUN dotnet restore "FamilyTaskManager.Worker/FamilyTaskManager.Worker.csproj"
RUN dotnet build "FamilyTaskManager.Worker/FamilyTaskManager.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FamilyTaskManager.Worker/FamilyTaskManager.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FamilyTaskManager.Worker.dll"]
```

```yaml
# docker-compose.yml
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

  worker:
    build:
      context: .
      dockerfile: src/FamilyTaskManager.Worker/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=FamilyTaskManager;Username=postgres;Password=${DB_PASSWORD}"
    depends_on:
      - postgres
    restart: unless-stopped

  bot:
    build:
      context: .
      dockerfile: src/FamilyTaskManager.Bot/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=FamilyTaskManager;Username=postgres;Password=${DB_PASSWORD}"
      Bot__BotToken: ${TELEGRAM_BOT_TOKEN}
      Bot__BotUsername: ${TELEGRAM_BOT_USERNAME}
    depends_on:
      - postgres
    restart: unless-stopped

volumes:
  postgres_data:
```

### 8. Мониторинг и Health Checks (1-2 дня)

```csharp
// Health check для Worker
public class QuartzHealthCheck : IHealthCheck
{
    private readonly IScheduler _scheduler;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        if (!_scheduler.IsStarted)
            return HealthCheckResult.Unhealthy("Quartz scheduler is not started");
        
        var runningJobs = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        var data = new Dictionary<string, object>
        {
            { "running_jobs", runningJobs.Count },
            { "scheduler_name", _scheduler.SchedulerName }
        };
        
        return HealthCheckResult.Healthy("Quartz scheduler is running", data);
    }
}

// Регистрация
builder.Services.AddHealthChecks()
    .AddCheck<QuartzHealthCheck>("quartz")
    .AddNpgSql(connectionString);
```

### 9. Timezone поддержка (1 день)

```csharp
// Обновить TaskInstanceCreatorJob
var familyTimezone = TimeZoneInfo.FindSystemTimeZoneById(family.Timezone);
var cronExpression = new CronExpression(template.Schedule)
{
    TimeZone = familyTimezone
};
var nextOccurrence = cronExpression.GetTimeAfter(DateTimeOffset.UtcNow);
```

## 📅 Рекомендуемый план

### Неделя 1 (21-27 ноября)
- ✅ День 1-2: Реализация Worker (завершено)
- ✅ День 3-4: Telegram уведомления (завершено)
- ✅ День 5: Система invite-кодов (завершено)
- ⏳ День 6-7: Создание задач через бота

### Неделя 2 (28 ноября - 4 декабря)
- ⏳ День 1-3: Создание задач через бота
- ⏳ День 4-5: Domain Event Handlers
- ⏳ День 6-7: Автоматическое создание TaskTemplate

### Неделя 3 (5-11 декабря)
- ⏳ День 1-3: Unit и Integration тесты
- ⏳ День 4-5: Docker и Docker Compose
- ⏳ День 6-7: Мониторинг и финальное тестирование

### Неделя 4 (12-18 декабря)
- ⏳ Production deployment
- ⏳ Документация для пользователей
- ⏳ Beta тестирование
- ⏳ Сбор обратной связи

## 🎯 Критерии готовности к запуску

### Must Have (обязательно)
- ✅ Telegram Bot работает
- ✅ Worker создает задачи
- ✅ Worker пересчитывает настроение
- ✅ Уведомления работают
- ✅ Invite codes реализованы
- ⏳ Можно создавать задачи через бота

### Should Have (желательно)
- ⏳ Domain Events обрабатываются
- ⏳ Автоматические TaskTemplate
- ⏳ Unit тесты покрывают 60%+
- ⏳ Docker готов к deployment

### Nice to Have (можно отложить)
- ⏳ API endpoints
- ⏳ Фото для задач
- ⏳ Расширенная статистика
- ⏳ Мониторинг dashboard

## 📞 Контакты и ресурсы

**Документация**:
- [Техническое задание](docs/MVP1/ТЗ%20MVP1.md)
- [Worker Quick Start](src/FamilyTaskManager.Worker/QUICK_START.md)
- [Bot Quick Start](src/FamilyTaskManager.Bot/QUICK_START.md)
- [Running the System](RUNNING_THE_SYSTEM.md)

**Полезные ссылки**:
- Quartz.NET: https://www.quartz-scheduler.net/
- Telegram Bot API: https://core.telegram.org/bots/api
- Cron Expression Generator: https://www.freeformatter.com/cron-expression-generator-quartz.html

**Следующая задача**: Создание задач через бота (Conversation Flow)

**Документация**:
- [Telegram Notifications](TELEGRAM_NOTIFICATIONS.md) - полная документация по уведомлениям
- [Invite System](INVITE_SYSTEM.md) - полная документация по системе приглашений
