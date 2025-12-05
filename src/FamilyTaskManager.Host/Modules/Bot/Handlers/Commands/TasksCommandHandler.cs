using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class TasksCommandHandler(IMediator mediator)
{
  public async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Guid userId,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get active tasks
    var getTasksQuery = new GetActiveTasksQuery(session.CurrentFamilyId.Value, userId);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    if (!tasksResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.TasksLoadError,
        cancellationToken: cancellationToken);
      return;
    }

    var tasks = tasksResult.Value;

    if (!tasks.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Messages.NoActiveTasks,
        cancellationToken: cancellationToken);
      return;
    }

    // Group tasks by status
    var activeTasks = tasks.Where(t => t.Status == TaskStatus.Active).ToList();
    var inProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();

    var messageText = "✅ *Наши задачи*\n\n";

    if (activeTasks.Any())
    {
      messageText += "*Доступные задачи:*\n";
      foreach (var task in activeTasks)
      {
        var overdueMarker = task.DueAtLocal < DateTime.Now ? "⚠️" : "";
        var spotEmoji = SpotDisplay.GetEmoji(task.SpotType);
        messageText += $"{overdueMarker} *{task.Title}*\n";
        messageText += $"   {spotEmoji} {task.SpotName} | {task.Points.ToStars()}\n";
        messageText += $"   📅 До: {task.DueAtLocal:dd.MM.yyyy HH:mm}\n\n";
      }
    }

    if (inProgressTasks.Any())
    {
      messageText += "\n*В работе:*\n";
      foreach (var task in inProgressTasks)
      {
        var spotEmoji = SpotDisplay.GetEmoji(task.SpotType);
        messageText += $"🔄 *{task.Title}*\n";
        messageText += $"   {spotEmoji} {task.SpotName} | {task.Points.ToStars()}\n";
        if (!string.IsNullOrEmpty(task.StartedByUserName)) messageText += $"   👤 Взял(а): {task.StartedByUserName}\n";

        messageText += "\n";
      }
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var task in activeTasks.Take(10)) // Limit to 10 tasks
      buttons.Add([InlineKeyboardButton.WithCallbackData($"✋ Взять: {task.Title}", $"task_take_{task.Id}")]);

    foreach (var task in inProgressTasks.Where(t => t.StartedByUserId == userId).Take(5))
    {
      buttons.Add([InlineKeyboardButton.WithCallbackData($"✅ Выполнить: {task.Title}", $"task_complete_{task.Id}")]);
      buttons.Add([InlineKeyboardButton.WithCallbackData($"❌ Отказаться: {task.Title}", $"task_cancel_{task.Id}")]);
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: buttons.Any() ? new InlineKeyboardMarkup(buttons) : null,
      cancellationToken: cancellationToken);
  }
}
