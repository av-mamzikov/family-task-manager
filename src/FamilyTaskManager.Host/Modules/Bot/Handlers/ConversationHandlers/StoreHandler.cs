using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class StoreHandler(
  ILogger<StoreHandler> logger)
  : BaseConversationHandler(logger), IConversationHandler
{
  public Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task HandleCallbackAsync(ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.IsCallbackOf(CallbackData.Store.List))
      await HandleListCallbackAsync(botClient, chatId, message, callbackParts, session, fromUser, cancellationToken);
  }

  private async Task HandleListCallbackAsync(ITelegramBotClient botClient, long chatId, Message? message,
    string[] callbackParts, UserSession session, User fromUser, CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "Магазин в разработке",
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }
}
