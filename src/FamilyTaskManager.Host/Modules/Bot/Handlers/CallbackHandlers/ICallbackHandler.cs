using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public interface ICallbackHandler
{
  Task Handle(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken);
}
