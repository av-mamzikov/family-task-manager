using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TaskBrowsingHandler(
  ILogger<TaskBrowsingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger, mediator), IConversationHandler
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
    if (callbackParts.Length < 2) return;

    var taskAction = callbackParts[1];

    if (callbackParts.Length < 3) return;

    if (!Guid.TryParse(callbackParts[2], out var taskId)) return;

    await (taskAction switch
    {
      CallbackActions.Take => HandleTakeTaskAsync(botClient, chatId, message, taskId,
        session, cancellationToken),
      CallbackActions.Complete => HandleCompleteTaskAsync(botClient, chatId, message,
        taskId, session, cancellationToken),
      CallbackActions.Cancel => HandleCancelTaskAsync(botClient, chatId, message, taskId,
        session, cancellationToken),
      _ => Task.CompletedTask
    });
  }

  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    await sendMainMenuAction();
    session.ClearState();
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
    var result = await Mediator.Send(takeTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var getTaskResult = await Mediator.Send(
      new GetTaskByIdQuery(taskId, session.CurrentFamilyId ?? Guid.Empty), cancellationToken);
    var task = getTaskResult.IsSuccess ? getTaskResult.Value : null;

    await botClient.SendOrEditMessageAsync(
      chatId,
      messageId,
      $" ✅ Задача взята в работу!\n\n{task?.Title} {task?.Points.ToStars()}\n",
      ParseMode.Markdown,
      new([
        [
          InlineKeyboardButton.WithCallbackData("✅ Выполнить", CallbackData.Task.Complete(task!.Id)),
          InlineKeyboardButton.WithCallbackData("❌ Отказаться", CallbackData.Task.Cancel(task.Id))
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
    var result = await Mediator.Send(completeTaskCommand, cancellationToken);

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

  private async Task HandleCancelTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var cancelTaskCommand = new CancelTaskCommand(taskId, session.UserId);
    var result = await Mediator.Send(cancelTaskCommand, cancellationToken);

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
}
