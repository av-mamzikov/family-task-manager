using FamilyTaskManager.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot;

/// <summary>
/// Hosted service wrapper for Telegram Bot with Long Polling
/// </summary>
public class TelegramBotHostedService : BackgroundService
{
  private readonly ITelegramBotClient _botClient;
  private readonly IUpdateHandler _updateHandler;
  private readonly ILogger<TelegramBotHostedService> _logger;

  public TelegramBotHostedService(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    ILogger<TelegramBotHostedService> logger)
  {
    _botClient = botClient;
    _updateHandler = updateHandler;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var me = await _botClient.GetMeAsync(stoppingToken);
    _logger.LogInformation("Bot Module started: @{Username}", me.Username);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = Array.Empty<UpdateType>(),
      ThrowPendingUpdates = true
    };

    await _botClient.ReceiveAsync(
      _updateHandler,
      receiverOptions,
      stoppingToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Bot Module stopping...");
    await base.StopAsync(cancellationToken);
  }
}
