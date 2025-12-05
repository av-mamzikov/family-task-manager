using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public interface IConversationHandler
{
  Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken);

  Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken);

  Task HandleCancelAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken);

  Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken);
}
