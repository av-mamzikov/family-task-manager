using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public abstract class BaseConversationHandler(ILogger logger, IMediator mediator)
{
  protected readonly ILogger Logger = logger;
  protected readonly IMediator Mediator = mediator;

  protected async Task SendErrorAndClearStateAsync(
    ITelegramBotClient botClient,
    long chatId,
    UserSession session,
    string errorMessage,
    CancellationToken cancellationToken)
  {
    session.ClearState();
    await botClient.SendTextMessageAsync(
      chatId,
      errorMessage,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  protected async Task SendValidationErrorAsync(
    ITelegramBotClient botClient,
    long chatId,
    string errorMessage,
    string hint,
    IReplyMarkup? keyboard,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      errorMessage + hint,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);

  protected bool TryGetSessionData<T>(
    UserSession session,
    string key,
    out T value) where T : notnull
  {
    if (session.Data.TryGetValue(key, out var obj) && obj is T typedValue)
    {
      value = typedValue;
      return true;
    }

    value = default!;
    return false;
  }
}
