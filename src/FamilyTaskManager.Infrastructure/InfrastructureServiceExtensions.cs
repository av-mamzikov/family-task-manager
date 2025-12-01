using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Behaviors;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Database;
using FamilyTaskManager.Infrastructure.DomainEvents;
using FamilyTaskManager.Infrastructure.Interfaces;
using FamilyTaskManager.Infrastructure.Jobs;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.Infrastructure.Notifications.Handlers;
using FamilyTaskManager.Infrastructure.Services;
using Mediator;
using Quartz;

namespace FamilyTaskManager.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration config,
    ILogger? logger = null)
  {
    logger ??= LoggerFactory.Create(builder => { })
      .CreateLogger(nameof(InfrastructureServiceExtensions));
    // Get database connection string using shared logic
    var connectionString = config.GetDatabaseConnectionString();

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      // Use PostgreSQL if DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseNpgsql(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
      .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // Register universal read-only repository for any entity
    services.AddScoped(typeof(IReadOnlyEntityRepository<>), typeof(EfReadOnlyEntityRepository<>));

    // Register infrastructure repository for non-domain entities (outbox, audit logs, etc.)
    services.AddScoped(typeof(IInfrastructureRepository<>), typeof(EfInfrastructureRepository<>));

    // Register Mediator Pipeline Behaviors
    services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(MediatorLoggingBehavior<,>));
    logger.LogInformation("Mediator pipeline behaviors registered");

    // Telegram Bot Client (from configuration)
    services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
    logger.LogInformation("Telegram notification service registered");

    // Register TaskCreated notification outbox handler for batched delivery
    services.AddScoped<INotificationHandler<TaskCreatedEvent>,
      TaskCreatedNotificationOutboxHandler>();

    // Register TaskReminder notification outbox handler for batched delivery
    services.AddScoped<INotificationHandler<TaskReminderDueEvent>,
      TaskReminderNotificationOutboxHandler>();

    // Register outbox event handlers for processing batched notifications
    services.AddScoped<IOutboxEventHandler, TaskCreatedOutboxHandler>();
    services.AddScoped<IOutboxEventHandler, TaskReminderOutboxHandler>();
    logger.LogInformation("Outbox handlers registered (TaskCreated, TaskReminder)");

    // Register TimeZone Service
    services.AddScoped<ITimeZoneService, TimeZoneService>();

    // Register Quartz Schema Initializer
    services.AddScoped<IQuartzSchemaInitializer, QuartzSchemaInitializer>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }

  /// <summary>
  ///   Registers infrastructure-specific Quartz jobs.
  ///   Must be called inside AddQuartz configuration in Host.
  /// </summary>
  public static void AddInfrastructureJobs(
    this IServiceCollectionQuartzConfigurator quartz,
    ILogger? logger = null)
  {
    logger?.LogInformation("Registering Infrastructure jobs...");

    // OutboxDispatcherJob - runs every 1 minute
    // Processes batched notifications (TaskCreated, TaskReminder)
    var outboxJobKey = new JobKey("OutboxDispatcherJob");
    quartz.AddJob<OutboxDispatcherJob>(opts => opts.WithIdentity(outboxJobKey));
    quartz.AddTrigger(opts => opts
      .ForJob(outboxJobKey)
      .WithIdentity("OutboxDispatcherJob-trigger")
      .WithCronSchedule("0 */1 * * * ?") // Every 1 minute
      .WithDescription("Processes batched notifications from outbox"));

    logger?.LogInformation("Infrastructure jobs registered: OutboxDispatcherJob");
  }
}
