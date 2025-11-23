using FamilyTaskManager.Host.Modules.Bot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

public interface ITelegramBotService
{
  Task StartAsync(CancellationToken cancellationToken);
  Task StopAsync(CancellationToken cancellationToken);
  Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}

public class TelegramBotService(
  ITelegramBotClient botClient,
  IServiceScopeFactory scopeFactory,
  ILogger<TelegramBotService> logger)
  : ITelegramBotService
{
  private CancellationTokenSource? _cts;

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
      ThrowPendingUpdates = true
    };

    var me = await botClient.GetMeAsync(cancellationToken);
    logger.LogInformation("Bot started: @{BotUsername}", me.Username);

    // Create a scope for each update to resolve scoped handlers
    botClient.StartReceiving(
      new ScopedUpdateHandler(scopeFactory),
      receiverOptions,
      _cts.Token);
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _cts?.Cancel();
    logger.LogInformation("Bot stopped");
    return Task.CompletedTask;
  }

  public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      text,
      cancellationToken: cancellationToken);
  }
}
