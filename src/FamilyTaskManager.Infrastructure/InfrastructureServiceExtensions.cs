using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.Infrastructure.Behaviors;
using FamilyTaskManager.Infrastructure.Services;
using FamilyTaskManager.Core.Interfaces;
using Telegram.Bot;
using Mediator;

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
    // Try to get connection strings in order of priority:
    // 1. "FamilyTaskManager" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional PostgreSQL connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = config.GetConnectionString("FamilyTaskManager")
                               ?? config.GetConnectionString("DefaultConnection") 
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();
      
      // Use PostgreSQL if Aspire or DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString("FamilyTaskManager") != null || 
          config.GetConnectionString("DefaultConnection") != null)
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

    // Register Mediator Pipeline Behaviors
    services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(MediatorLoggingBehavior<,>));
    logger.LogInformation("Mediator pipeline behaviors registered");

    // Telegram Bot Client (from configuration)
    var botToken = config["Bot:BotToken"];
    if (!string.IsNullOrEmpty(botToken))
    {
      services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(botToken));
      services.AddScoped<TelegramNotificationService>();
      logger.LogInformation("Telegram notification service registered");
    }
    else
    {
      logger.LogWarning("Bot:BotToken not configured - Telegram notifications will not be available");
    }

    // Register Schedule Evaluator
    services.AddScoped<IScheduleEvaluator, QuartzScheduleEvaluator>();
    
    // Register TimeZone Service
    services.AddScoped<ITimeZoneService, TimeZoneService>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
