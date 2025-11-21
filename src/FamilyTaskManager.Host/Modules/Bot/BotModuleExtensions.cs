using FamilyTaskManager.Bot.Configuration;
using FamilyTaskManager.Bot.Handlers;
using FamilyTaskManager.Bot.Handlers.Commands;
using FamilyTaskManager.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace FamilyTaskManager.Host.Modules.Bot;

public static class BotModuleExtensions
{
  public static IServiceCollection AddBotModule(this IServiceCollection services, IConfiguration configuration)
  {
    // Bot Configuration
    var botConfig = configuration.GetSection("Bot").Get<BotConfiguration>();
    if (botConfig == null || string.IsNullOrEmpty(botConfig.BotToken))
    {
      throw new InvalidOperationException("Bot configuration is missing. Please configure Bot:BotToken in appsettings.json or user secrets.");
    }

    services.AddSingleton(botConfig);

    // Telegram Bot Client
    services.AddSingleton<ITelegramBotClient>(sp =>
    {
      var config = sp.GetRequiredService<BotConfiguration>();
      return new TelegramBotClient(config.BotToken);
    });

    // Bot Services
    services.AddSingleton<SessionManager>();
    services.AddSingleton<ITelegramBotService, TelegramBotService>();
    
    // Hosted Service for Long Polling
    services.AddHostedService<TelegramBotHostedService>();

    // Handlers
    services.AddScoped<IUpdateHandler, UpdateHandler>();
    services.AddScoped<CommandHandler>();
    services.AddScoped<CallbackQueryHandler>();
    services.AddScoped<FamilyCommandHandler>();
    services.AddScoped<TasksCommandHandler>();
    services.AddScoped<PetCommandHandler>();
    services.AddScoped<StatsCommandHandler>();

    return services;
  }
}
