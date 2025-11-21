# Следующие шаги после реализации Worker

## ✅ Что сделано

1. **Quartz.NET Worker** - полностью реализован и готов к использованию
2. **Use Cases для Worker** - все необходимые команды и запросы
3. **Jobs** - TaskInstanceCreator, TaskReminder, PetMoodCalculator
4. **Документация** - полная документация и инструкции
5. **Скрипты** - автоматизация запуска и тестирования
6. **Telegram уведомления** - отправка уведомлений о задачах и настроении питомца


**Архитектура уведомлений**:
- ✅ Рефакторинг в Clean Architecture с Domain Events (ЗАВЕРШЕНО)
- ✅ TelegramNotificationService перемещен в Infrastructure
- ✅ Уведомления отправляются через Event Handlers
- ✅ UseCases не зависят от деталей уведомлений
- ⏳ Добавить unit тесты для notification service

### 2. Система invite-кодов (2-3 дня)

**Почему важно**: Без этого невозможно добавлять участников в семью

**Что делать**:

```csharp
// 1. Создать таблицу Invitations
public class Invitation : EntityBase<Invitation, Guid>
{
    public Guid FamilyId { get; private set; }
    public FamilyRole Role { get; private set; }
    public string Code { get; private set; } // Уникальный код
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
}

// 2. Создать Use Cases
public record CreateInviteCodeCommand(Guid FamilyId, FamilyRole Role) : ICommand<Result<string>>;
public record JoinByInviteCodeCommand(Guid UserId, string Code) : ICommand<Result<Guid>>;

// 3. Обновить /start команду
if (message.Text.StartsWith("/start invite_"))
{
    var code = message.Text.Replace("/start invite_", "");
    var result = await _mediator.Send(new JoinByInviteCodeCommand(user.Id, code));
    // ...
}

// 4. Добавить UI в /family
// Кнопка "Пригласить участника" → выбор роли → генерация ссылки
```

**Файлы для создания/изменения**:
- `src/FamilyTaskManager.Core/FamilyAggregate/Invitation.cs` (создать)
- `src/FamilyTaskManager.Infrastructure/Data/Config/InvitationConfiguration.cs` (создать)
- `src/FamilyTaskManager.UseCases/Families/CreateInviteCode.cs` (создать)
- `src/FamilyTaskManager.UseCases/Families/JoinByInviteCode.cs` (создать)
- `src/FamilyTaskManager.Bot/Handlers/Commands/FamilyCommandHandler.cs` (обновить)
- Миграция БД

### 3. Создание задач через бота (3-4 дня)

**Почему важно**: Админы должны создавать задачи без SQL

**Что делать**:

```csharp
// 1. Добавить Conversation Flow для создания задачи
public class CreateTaskConversation
{
    public enum State
    {
        SelectType,      // Разовая или периодическая
        EnterTitle,      // Ввод названия
        EnterPoints,     // Ввод очков
        SelectPet,       // Выбор питомца
        EnterSchedule,   // Для периодических: ввод cron
        Confirm          // Подтверждение
    }
}

// 2. Обновить CommandHandler
case "create_task":
    session.ConversationState = ConversationState.CreatingTask;
    session.ConversationData["step"] = CreateTaskConversation.State.SelectType;
    // Показать кнопки: "Разовая" / "Периодическая"
    break;

// 3. Добавить валидацию Cron
private bool IsValidCronExpression(string cron)
{
    try
    {
        var expression = new CronExpression(cron);
        return true;
    }
    catch
    {
        return false;
    }
}
```

**Файлы для изменения**:
- `src/FamilyTaskManager.Bot/Models/UserSession.cs` (добавить CreateTaskConversation)
- `src/FamilyTaskManager.Bot/Handlers/CommandHandler.cs` (обновить)
- `src/FamilyTaskManager.Bot/Handlers/Commands/TasksCommandHandler.cs` (обновить)

## 🎯 Приоритет 2: Важно для стабильности

### 4. Domain Event Handlers (2 дня)

**Что делать**:

```csharp
// 1. Создать обработчики
public class TaskCompletedEventHandler : INotificationHandler<TaskCompletedEvent>
{
    public async Task Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Начислить очки участнику
        // 2. Обновить настроение питомца
        // 3. Записать в историю
        // 4. Отправить уведомления семье
    }
}

// 2. Зарегистрировать в DI
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(TaskCompletedEventHandler).Assembly);
});
```

**Файлы для создания**:
- `src/FamilyTaskManager.Infrastructure/DomainEvents/TaskCompletedEventHandler.cs`
- `src/FamilyTaskManager.Infrastructure/DomainEvents/PetMoodUpdatedEventHandler.cs`
- `src/FamilyTaskManager.Infrastructure/DomainEvents/MemberAddedEventHandler.cs`

### 5. Автоматическое создание TaskTemplate (1-2 дня)

**Что делать**:

```csharp
// 1. Создать глобальные шаблоны
public static class PetTaskTemplates
{
    public static List<TaskTemplateDto> GetTemplatesForPetType(PetType type)
    {
        return type switch
        {
            PetType.Cat => new List<TaskTemplateDto>
            {
                new("Покормить кота", 10, "0 0 9,20 * * ?"),
                new("Поменять воду", 5, "0 0 9 * * ?"),
                new("Почистить лоток", 15, "0 0 20 * * ?"),
            },
            PetType.Dog => new List<TaskTemplateDto>
            {
                new("Покормить собаку", 10, "0 0 8,18 * * ?"),
                new("Выгулять собаку", 20, "0 0 8,14,20 * * ?"),
                new("Поменять воду", 5, "0 0 9 * * ?"),
            },
            // ...
        };
    }
}

// 2. Обновить CreatePetHandler
public async ValueTask<Result<Guid>> Handle(CreatePetCommand request, CancellationToken cancellationToken)
{
    var pet = new Pet(request.FamilyId, request.Type, request.Name);
    await _petRepository.AddAsync(pet, cancellationToken);
    
    // Создать шаблоны задач
    var templates = PetTaskTemplates.GetTemplatesForPetType(request.Type);
    foreach (var template in templates)
    {
        var taskTemplate = new TaskTemplate(
            request.FamilyId,
            pet.Id,
            template.Title,
            template.Points,
            template.Schedule,
            request.CreatedBy
        );
        await _templateRepository.AddAsync(taskTemplate, cancellationToken);
    }
    
    await _petRepository.SaveChangesAsync(cancellationToken);
    return Result.Success(pet.Id);
}
```

**Файлы для создания/изменения**:
- `src/FamilyTaskManager.Core/PetAggregate/PetTaskTemplates.cs` (создать)
- `src/FamilyTaskManager.UseCases/Pets/CreatePet.cs` (обновить)

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
- ⏳ День 5-6: Система invite-кодов
- ⏳ День 7: Тестирование и багфиксы

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
- ⏳ Invite codes реализованы
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

**Следующая задача**: Реализация системы invite-кодов для приглашения участников в семью

**Документация**:
- [Telegram Notifications](TELEGRAM_NOTIFICATIONS.md) - полная документация по уведомлениям
