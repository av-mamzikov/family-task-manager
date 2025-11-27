using FamilyTaskManager.Host.Modules.Bot.Configuration;
using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
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
      {
        throw new InvalidOperationException(
          "Bot configuration is missing. Please configure Bot:BotToken in appsettings.json or user secrets.");
      }

      return new TelegramBotClient(config.BotToken);
    });

    // Bot Services
    services.AddSingleton<ISessionManager, SessionManager>();
    services.AddScoped<IUserRegistrationService, UserRegistrationService>();

    // Hosted Service for Long Polling
    services.AddHostedService<TelegramBotHostedService>();

    // Handlers
    services.AddScoped<IUpdateHandler, UpdateHandler>();
    services.AddScoped<IMessageHandler, MessageHandler>();
    services.AddScoped<ICallbackQueryHandler, CallbackQueryHandler>();

    // Command Handlers
    services.AddScoped<FamilyCommandHandler>();
    services.AddScoped<TasksCommandHandler>();
    services.AddScoped<PetCommandHandler>();
    services.AddScoped<StatsCommandHandler>();
    services.AddScoped<TemplateCommandHandler>();

    // Conversation Handlers
    services.AddScoped<IConversationRouter, ConversationRouter>();
    services.AddScoped<FamilyCreationHandler>();
    services.AddScoped<FamilyMembersHandler>();
    services.AddScoped<PetCreationHandler>();
    services.AddScoped<TaskCreationHandler>();
    services.AddScoped<TemplateCreationHandler>();
    services.AddScoped<TemplateEditHandler>();

    // Callback Handlers
    services.AddScoped<ICallbackRouter, CallbackRouter>();
    services.AddScoped<FamilyCallbackHandler>();
    services.AddScoped<PetCallbackHandler>();
    services.AddScoped<TaskCallbackHandler>();
    services.AddScoped<TemplateCallbackHandler>();
    services.AddScoped<TimezoneCallbackHandler>();

    logger?.LogInformation("Bot Module registered: Telegram Bot with Long Polling");

    return services;
  }
}
