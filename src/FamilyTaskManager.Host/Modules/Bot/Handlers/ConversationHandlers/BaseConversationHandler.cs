using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public abstract class BaseConversationHandler(ILogger logger)
{
  protected readonly ILogger Logger = logger;

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
    Message? message,
    string errorMessage,
    CancellationToken cancellationToken) =>
    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      errorMessage,
      cancellationToken: cancellationToken);
}
