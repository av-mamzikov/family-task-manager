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

public class TelegramBotService : ITelegramBotService
{
  private readonly ITelegramBotClient _botClient;
  private readonly ILogger<TelegramBotService> _logger;
  private readonly IServiceScopeFactory _scopeFactory;
  private CancellationTokenSource? _cts;

  public TelegramBotService(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramBotService> logger)
  {
    _botClient = botClient;
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    var receiverOptions = new ReceiverOptions
    {
      AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }, ThrowPendingUpdates = true
    };

    var me = await _botClient.GetMeAsync(cancellationToken);
    _logger.LogInformation("Bot started: @{BotUsername}", me.Username);

    // Create a scope for each update to resolve scoped handlers
    _botClient.StartReceiving(
      new ScopedUpdateHandler(_scopeFactory),
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
