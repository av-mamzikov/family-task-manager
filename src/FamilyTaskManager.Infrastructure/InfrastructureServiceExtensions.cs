using Ardalis.SharedKernel;
using FamilyTaskManager.Infrastructure.Behaviors;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Data.Queries;
using FamilyTaskManager.Infrastructure.Database;
using FamilyTaskManager.Infrastructure.Interfaces;
using FamilyTaskManager.Infrastructure.Jobs;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.Infrastructure.Services;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;
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

    services.AddScoped<OutboxInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    if (string.IsNullOrEmpty(connectionString))
      throw new InvalidOperationException("DefaultConnection is not available");

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      options.UseNpgsql(connectionString);
      options.AddInterceptors(provider.GetRequiredService<OutboxInterceptor>());
    });

    services.AddScoped(typeof(IAppRepository<>), typeof(EfAppRepository<>))
      .AddScoped(typeof(IAppReadRepository<>), typeof(EfAppRepository<>));

    // Register universal read-only repository for any entity
    services.AddScoped(typeof(IReadOnlyEntityRepository<>), typeof(EfReadOnlyEntityRepository<>));

    services.AddScoped<ITaskCompletionStatsQuery, TaskCompletionStatsQuery>();

    // Register Mediator Pipeline Behaviors
    services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(MediatorLoggingBehavior<,>));
    logger.LogInformation("Mediator pipeline behaviors registered");

    // Telegram Bot Client (from configuration)
    services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
    logger.LogInformation("Telegram notification service registered");

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

    var outboxJobKey = new JobKey("OutboxDispatcherJob");
    quartz.AddJob<OutboxDispatcherJob>(opts => opts.WithIdentity(outboxJobKey));
    quartz.AddTrigger(opts => opts
      .ForJob(outboxJobKey)
      .WithIdentity("OutboxDispatcherJob-trigger")
      .WithCronSchedule("*/10 * * * * ?")
      .WithDescription("Processes batched notifications from outbox"));

    logger?.LogInformation("Infrastructure jobs registered: OutboxDispatcherJob");
  }
}
