using FamilyTaskManager.Host.Modules.Bot.Configuration;
using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace FamilyTaskManager.Host.Modules.Bot;

public static class BotModuleExtensions
{
  public static IServiceCollection AddBotModule(
    this IServiceCollection services,
    IConfiguration configuration,
    ILogger? logger = null)
  {
    logger?.LogInformation("Registering Bot Module...");

    // Bot Configuration
    services.AddSingleton(configuration.GetSection("Bot").Get<BotConfiguration>() ?? new BotConfiguration());

    // Telegram Bot Client
    services.AddSingleton<ITelegramBotClient>(sp =>
    {
      var config = sp.GetRequiredService<BotConfiguration>();
      if (string.IsNullOrEmpty(config.BotToken))
        throw new InvalidOperationException(
          "Bot configuration is missing. Please configure Bot:BotToken in appsettings.json or user secrets.");

      return new TelegramBotClient(config.BotToken);
    });

    // Bot Services
    services.AddSingleton<BotInfoService>();
    services.AddScoped<ISessionManager, SessionManager>();

    // Hosted Service for Long Polling
    services.AddHostedService<TelegramBotHostedService>();

    // Handlers
    services.AddScoped<IUpdateHandler, UpdateHandler>();

    // Conversation Handlers
    var conversationHandlerInterface = typeof(IConversationHandler);
    var conversationHandlerAssembly = conversationHandlerInterface.Assembly;
    var conversationHandlerTypes = conversationHandlerAssembly
      .GetTypes()
      .Where(t =>
        t.IsClass &&
        !t.IsAbstract &&
        !t.IsGenericTypeDefinition &&
        conversationHandlerInterface.IsAssignableFrom(t));

    foreach (var handlerType in conversationHandlerTypes)
    {
      services.AddScoped(handlerType);
      services.AddScoped(conversationHandlerInterface, handlerType);
    }

    logger?.LogInformation("Bot Module registered: Telegram Bot with Long Polling");

    return services;
  }
}
