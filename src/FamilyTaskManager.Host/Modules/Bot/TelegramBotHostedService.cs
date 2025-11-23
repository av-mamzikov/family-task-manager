using FamilyTaskManager.Host.Modules.Bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot;

/// <summary>
///   Hosted service wrapper for Telegram Bot with Long Polling
/// </summary>
public class TelegramBotHostedService : BackgroundService
{
  private readonly ITelegramBotClient _botClient;
  private readonly ILogger<TelegramBotHostedService> _logger;
  private readonly IServiceScopeFactory _scopeFactory;

  public TelegramBotHostedService(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramBotHostedService> logger)
  {
    _botClient = botClient;
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var me = await _botClient.GetMeAsync(stoppingToken);
    _logger.LogInformation("Bot Module started: @{Username}", me.Username);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = Array.Empty<UpdateType>(), ThrowPendingUpdates = true
    };

    await _botClient.ReceiveAsync(
      new ScopedUpdateHandler(_scopeFactory),
      receiverOptions,
      stoppingToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Bot Module stopping...");
    await base.StopAsync(cancellationToken);
  }
}
