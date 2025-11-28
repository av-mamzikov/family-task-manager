using FamilyTaskManager.Host.Modules.Bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

/// <summary>
///   Hosted service wrapper for Telegram Bot with Long Polling
/// </summary>
public class TelegramBotHostedService(
  ITelegramBotClient botClient,
  IServiceScopeFactory scopeFactory,
  BotInfoService botInfoService,
  ILogger<TelegramBotHostedService> logger)
  : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (true)
    {
      try
      {
        logger.LogInformation("Starting bot module...");
        var me = await botClient.GetMeAsync(stoppingToken);

        if (string.IsNullOrEmpty(me.Username))
          throw new InvalidOperationException("Failed to get bot username from Telegram API.");

        botInfoService.SetBotInfo(me.Username);
        logger.LogInformation("Bot Module started: @{Username}", me.Username);

        var receiverOptions = new ReceiverOptions
        {
          AllowedUpdates = [],
          ThrowPendingUpdates = true
        };

        await botClient.ReceiveAsync(
          new ScopedUpdateHandler(scopeFactory),
          receiverOptions,
          stoppingToken);
      }
      catch (Exception ex)
      {
        logger.LogCritical(ex, "Ошибка связи с телеграм-ботом");
        await Task.Delay(5000, stoppingToken);
      }
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    logger.LogInformation("Bot Module stopping...");
    await base.StopAsync(cancellationToken);
  }
}
