using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public interface ICallbackQueryHandler
{
  Task HandleCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, UserSession session,
    CancellationToken cancellationToken);
}
