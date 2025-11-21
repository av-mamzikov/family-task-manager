using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.UseCases.Notifications;
using Telegram.Bot;

namespace FamilyTaskManager.Infrastructure;
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    // Try to get connection strings in order of priority:
    // 1. "cleanarchitecture" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional SQL Server connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = config.GetConnectionString("cleanarchitecture")
                               ?? config.GetConnectionString("DefaultConnection") 
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();
      
      // Use SQL Server if Aspire or DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString("cleanarchitecture") != null || 
          config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }
      
      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // Telegram Bot Client (from configuration)
    var botToken = config["Bot:BotToken"];
    if (!string.IsNullOrEmpty(botToken))
    {
      services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(botToken));
      services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
      logger.LogInformation("Telegram notification service registered");
    }
    else
    {
      logger.LogWarning("Bot:BotToken not configured - Telegram notifications will not be available");
    }

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
