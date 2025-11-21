using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Bot.Handlers;

public interface ICommandHandler
{
  Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}
