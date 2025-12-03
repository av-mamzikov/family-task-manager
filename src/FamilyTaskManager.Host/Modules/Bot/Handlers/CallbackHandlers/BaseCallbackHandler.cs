using Telegram.Bot;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public abstract class BaseCallbackHandler(ILogger logger, IMediator mediator)
{
  protected readonly ILogger Logger = logger;
  protected readonly IMediator Mediator = mediator;

  protected async Task SendErrorAsync(
    ITelegramBotClient botClient,
    long chatId,
    string errorMessage,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      errorMessage,
      cancellationToken: cancellationToken);

  protected async Task EditMessageWithErrorAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string errorMessage,
    CancellationToken cancellationToken) =>
    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      errorMessage,
      cancellationToken: cancellationToken);
}
