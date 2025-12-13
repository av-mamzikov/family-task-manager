using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using FamilyTaskManager.UseCases.Features.TasksManagement.Queries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TaskBrowsingHandler(
  ILogger<TaskBrowsingHandler> logger,
  IMediator mediator)
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
    if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.List))
      await HandleTaskListAsync(botClient, chatId, message, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.Take, out EncodedGuid takeTaskId))
      await HandleTakeTaskAsync(botClient, chatId, message, takeTaskId.Value, session, cancellationToken);
    if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.Complete, out EncodedGuid completeTaskId))
      await HandleCompleteTaskAsync(botClient, chatId, message, completeTaskId.Value, session, cancellationToken);
    if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.Refuse, out EncodedGuid cancelTaskId))
      await HandleRefuseTaskAsync(botClient, chatId, message, cancelTaskId.Value, session, cancellationToken);
    if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.Delete, out EncodedGuid deleteTaskId))
      await HandleDeleteTaskAsync(botClient, chatId, message, deleteTaskId.Value, session, cancellationToken);
  }

  private async Task HandleTaskListAsync(ITelegramBotClient botClient, long chatId, Message? message,
    UserSession session, CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
      return;

    // Get active tasks
    var getTasksQuery = new GetActiveTasksQuery(session.CurrentFamilyId.Value, session.UserId);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    if (!tasksResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.TasksLoadError,
        cancellationToken: cancellationToken);
      return;
    }

    var tasks = tasksResult.Value;

    if (!tasks.Any())
    {
      await botClient.SendTextMessageAsync(
        chatId,
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

    foreach (var task in activeTasks) // Limit to 10 tasks
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"✋ Взять: {task.Title}", CallbackData.TaskBrowsing.Take(task.Id))
      ]);

    foreach (var task in inProgressTasks.Where(t => t.StartedByUserId == session.UserId))
    {
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"✅ Выполнить: {task.Title}", CallbackData.TaskBrowsing.Complete(task.Id))
      ]);
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"❌ Отказаться: {task.Title}", CallbackData.TaskBrowsing.Refuse(task.Id))
      ]);
    }

    await botClient.SendTextMessageAsync(
      chatId,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: buttons.Any() ? new InlineKeyboardMarkup(buttons) : null,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTakeTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? messageId,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var takeTaskCommand = new TakeTaskCommand(taskId, session.UserId);
    var result = await mediator.Send(takeTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var getTaskResult = await mediator.Send(
      new GetTaskByIdQuery(taskId, session.CurrentFamilyId ?? Guid.Empty), cancellationToken);
    var task = getTaskResult.IsSuccess ? getTaskResult.Value : null;

    await botClient.SendOrEditMessageAsync(
      chatId,
      messageId,
      $" ✅ Задача взята в работу!\n\n{task?.Title} {task?.Points.ToStars()}\n",
      ParseMode.Markdown,
      new InlineKeyboardMarkup([
        [
          InlineKeyboardButton.WithCallbackData("✅ Выполнить", CallbackData.TaskBrowsing.Complete(task!.Id)),
          InlineKeyboardButton.WithCallbackData("❌ Отказаться", CallbackData.TaskBrowsing.Refuse(task.Id)),
          InlineKeyboardButton.WithCallbackData("🗑️ Удалить", CallbackData.TaskBrowsing.Delete(task.Id))
        ]
      ]),
      cancellationToken);
  }

  private async Task HandleCompleteTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var completeTaskCommand = new CompleteTaskCommand(taskId, session.UserId);
    var result = await mediator.Send(completeTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "🎉 Задача выполнена!\n\n⭐ Очки начислены!",
      cancellationToken: cancellationToken);
  }

  private async Task HandleRefuseTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var cancelTaskCommand = new RefuseTaskCommand(taskId, session.UserId);
    var result = await mediator.Send(cancelTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "✅ Вы отказались от задачи.\n\nЗадача снова доступна для всех участников семьи.",
      cancellationToken: cancellationToken);
  }

  private async Task HandleDeleteTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var cancelTaskCommand = new DeleteTaskCommand(taskId, session.UserId);
    var result = await mediator.Send(cancelTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "✅ Вы удалили задачу.",
      cancellationToken: cancellationToken);
  }
}
