using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;
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
        "❌ Сначала выберите активную семью через /family",
        cancellationToken: cancellationToken);
      return;
    }

    // Get active tasks
    var getTasksQuery = new GetActiveTasksQuery(session.CurrentFamilyId.Value);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    if (!tasksResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка загрузки задач",
        cancellationToken: cancellationToken);
      return;
    }

    var tasks = tasksResult.Value;

    if (!tasks.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "📋 Активных задач пока нет.\n\nАдминистратор может создать задачи через настройки.",
        cancellationToken: cancellationToken);
      return;
    }

    // Group tasks by status
    var activeTasks = tasks.Where(t => t.Status == TaskStatus.Active).ToList();
    var inProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();

    var messageText = "✅ *Активные задачи:*\n\n";

    if (activeTasks.Any())
    {
      messageText += "*Доступные задачи:*\n";
      foreach (var task in activeTasks)
      {
        var overdueMarker = task.DueAt < DateTime.UtcNow ? "⚠️" : "";
        messageText += $"{overdueMarker} *{task.Title}*\n";
        messageText += $"   🐾 {task.PetName} | ⭐ {task.Points} очков\n";
        messageText += $"   📅 До: {task.DueAt:dd.MM.yyyy HH:mm}\n\n";
      }
    }

    if (inProgressTasks.Any())
    {
      messageText += "\n*В работе:*\n";
      foreach (var task in inProgressTasks)
      {
        messageText += $"🔄 *{task.Title}*\n";
        messageText += $"   🐾 {task.PetName} | ⭐ {task.Points} очков\n\n";
      }
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>();
    
    foreach (var task in activeTasks.Take(10)) // Limit to 10 tasks
    {
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"✋ Взять: {task.Title}",
          $"task_take_{task.Id}")
      });
    }

    foreach (var task in inProgressTasks.Take(5))
    {
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"✅ Выполнить: {task.Title}",
          $"task_complete_{task.Id}")
      });
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
      replyMarkup: buttons.Any() ? new InlineKeyboardMarkup(buttons) : null,
      cancellationToken: cancellationToken);
  }
}
