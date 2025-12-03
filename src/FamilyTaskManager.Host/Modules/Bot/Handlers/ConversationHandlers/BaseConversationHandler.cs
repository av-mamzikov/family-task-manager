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
    await botClient.SendTextMessageAsync(
      chatId,
      errorMessage,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
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
}
