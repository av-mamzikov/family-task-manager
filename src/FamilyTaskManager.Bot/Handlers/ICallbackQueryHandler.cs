using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Bot.Handlers;

public interface ICallbackQueryHandler
{
  Task HandleCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}
