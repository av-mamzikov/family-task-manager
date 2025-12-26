using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate.DTOs;
using FamilyTaskManager.Core.Utils;
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
    else if (callbackParts.IsCallbackOf(CallbackData.TaskBrowsing.OtherList))
      await HandleOtherTasksListAsync(botClient, chatId, message, session, cancellationToken);
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
    var getTasksQuery = new GetMyAvailableTasksQuery(session.CurrentFamilyId.Value, session.UserId);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    if (!tasksResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.TasksLoadError,
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = "✅ *Мои задачи*\n\n";

    var tasks = tasksResult.Value;

    if (tasks.Count == 0)
      messageText += BotMessages.Messages.NoActiveTasks + "\n";

    // Group tasks by status
    var activeTasks = tasks.Where(t => t.Status == TaskStatus.Active).ToList();
    var inProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList();


    if (activeTasks.Count != 0)
    {
      messageText += "*Доступные задачи:*\n";
      foreach (var task in activeTasks) messageText += FormatTaskBlock(task);
    }

    if (inProgressTasks.Count != 0)
    {
      messageText += "\n*В работе:*\n";
      foreach (var task in inProgressTasks) messageText += FormatTaskBlock(task);
    }

    // Build inline keyboard
    var buttons = new List<InlineKeyboardButton[]>();

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("👀 Другие задачи", CallbackData.TaskBrowsing.OtherList())
    ]);

    foreach (var task in activeTasks)
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"✋ {task.SpotName}: {task.Title}",
          CallbackData.TaskBrowsing.Take(task.Id))
      ]);

    foreach (var task in inProgressTasks.Where(t => t.AssignedToUserId == session.UserId))
    {
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"✅: {task.SpotName}: {task.Title}",
          CallbackData.TaskBrowsing.Complete(task.Id))
      ]);
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"❌: {task.SpotName}: {task.Title}",
          CallbackData.TaskBrowsing.Refuse(task.Id))
      ]);
    }

    await botClient.SendTextMessageAsync(
      chatId,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: buttons.Any() ? new InlineKeyboardMarkup(buttons) : null,
      cancellationToken: cancellationToken);
  }

  private async Task HandleOtherTasksListAsync(ITelegramBotClient botClient, long chatId, Message? message,
    UserSession session, CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
      return;

    var query = new GetOtherTasksQuery(session.CurrentFamilyId.Value, session.UserId);
    var tasksResult = await mediator.Send(query, cancellationToken);

    if (!tasksResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.TasksLoadError,
        cancellationToken: cancellationToken);
      return;
    }

    var tasks = tasksResult.Value;

    var messageText = "👀 *Другие задачи*\n\n";

    if (!tasks.Any())
      messageText += "Пока никто не взял миссии в работу.";
    else
      foreach (var task in tasks)
        messageText += FormatTaskBlock(task);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      new InlineKeyboardMarkup([
        [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.TaskBrowsing.List())]
      ]),
      cancellationToken);
  }

  private static string FormatTaskBlock(TaskDto task)
  {
    var spotEmoji = SpotDisplay.GetEmoji(task.SpotType);
    var statusEmoji = task.Status == TaskStatus.InProgress ? "🔄" : "";
    var overdueMarker = task.Status == TaskStatus.Active && task.DueAtLocal < DateTime.Now ? "⚠️" : "";

    var text = $"{statusEmoji}{overdueMarker} *{task.SpotName} {task.Title}*\n";
    text += $"   {spotEmoji} {task.SpotName} | {task.Points.ToStars()}\n";

    if (task.Status == TaskStatus.Active)
      text += $"   📅 До: {task.DueAtLocal:dd.MM.yyyy HH:mm}\n";

    if (!string.IsNullOrEmpty(task.AssignedToUserName) && task.AssignedToUserTelegramId is not null)
      text +=
        $"   🦸 Герой миссии: {WikiHelper.GetUserLink(task.AssignedToUserName, task.AssignedToUserTelegramId.Value)}\n";
    else
      text += "   ⚔️ Миссия ждёт героя\n";

    text += "\n";
    return text;
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
          InlineKeyboardButton.WithCallbackData("✅", CallbackData.TaskBrowsing.Complete(task!.Id)),
          InlineKeyboardButton.WithCallbackData("❌", CallbackData.TaskBrowsing.Refuse(task.Id)),
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
    var refuseTaskCommand = new RefuseTaskCommand(taskId, session.UserId);
    var result = await mediator.Send(refuseTaskCommand, cancellationToken);

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
