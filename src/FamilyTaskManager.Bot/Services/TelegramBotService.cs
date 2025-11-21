using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FamilyTaskManager.Bot.Handlers;
using Microsoft.Extensions.Options;
using FamilyTaskManager.Bot.Configuration;

namespace FamilyTaskManager.Bot.Services;

public interface ITelegramBotService
{
  Task StartAsync(CancellationToken cancellationToken);
  Task StopAsync(CancellationToken cancellationToken);
  Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}

public class TelegramBotService : ITelegramBotService
{
  private readonly ITelegramBotClient _botClient;
  private readonly IUpdateHandler _updateHandler;
  private readonly ILogger<TelegramBotService> _logger;
  private CancellationTokenSource? _cts;

  public TelegramBotService(
    IOptions<BotConfiguration> botConfig,
    IUpdateHandler updateHandler,
    ILogger<TelegramBotService> logger)
  {
    _botClient = new TelegramBotClient(botConfig.Value.BotToken);
    _updateHandler = updateHandler;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = new[] 
      { 
        UpdateType.Message, 
        UpdateType.CallbackQuery 
      },
      ThrowPendingUpdates = true
    };

    var me = await _botClient.GetMeAsync(cancellationToken);
    _logger.LogInformation("Bot started: @{BotUsername}", me.Username);

    _botClient.StartReceiving(
      _updateHandler,
      receiverOptions,
      _cts.Token);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _cts?.Cancel();
    _logger.LogInformation("Bot stopped");
    return Task.CompletedTask;
  }

  public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
  {
    await _botClient.SendTextMessageAsync(
      chatId,
      text,
      cancellationToken: cancellationToken);
  }
}
