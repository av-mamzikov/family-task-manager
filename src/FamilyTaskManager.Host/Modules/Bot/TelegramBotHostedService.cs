using FamilyTaskManager.Host.Modules.Bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot;

/// <summary>
///   Hosted service wrapper for Telegram Bot with Long Polling
/// </summary>
public class TelegramBotHostedService(
  ITelegramBotClient botClient,
  IServiceScopeFactory scopeFactory,
  ILogger<TelegramBotHostedService> logger)
  : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var me = await botClient.GetMeAsync(stoppingToken);
    logger.LogInformation("Bot Module started: @{Username}", me.Username);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = Array.Empty<UpdateType>(),
      ThrowPendingUpdates = true
    };

    await botClient.ReceiveAsync(
      new ScopedUpdateHandler(scopeFactory),
      receiverOptions,
      stoppingToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    logger.LogInformation("Bot Module stopping...");
    await base.StopAsync(cancellationToken);
  }
}
