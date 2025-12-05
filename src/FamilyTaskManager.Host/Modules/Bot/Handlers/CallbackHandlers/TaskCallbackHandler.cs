using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class TaskCallbackHandler(
  ILogger<TaskCallbackHandler> logger,
  IMediator mediator)
  : BaseCallbackHandler(logger, mediator), ICallbackHandler
{
  public async Task Handle(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken) =>
    await HandleTaskActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken);

  public async Task HandleTaskActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2) return;

    var taskAction = parts[1];

    if (parts.Length < 3) return;

    var taskIdStr = parts[2];

    if (!Guid.TryParse(taskIdStr, out var taskId)) return;

    switch (taskAction)
    {
      case var _ when taskAction == CallbackActions.Take:
        await HandleTakeTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;

      case var _ when taskAction == CallbackActions.Complete:
        await HandleCompleteTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;

      case var _ when taskAction == CallbackActions.Cancel:
        await HandleCancelTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;
    }
  }

  private async Task HandleTakeTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Take task
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

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $" ✅ Задача взята в работу!\n\n{task?.Title} {task?.Points.ToStars()}\n",
      replyMarkup: new([
        [
          InlineKeyboardButton.WithCallbackData("✅ Выполнить", CallbackData.Task.Complete(task!.Id)),
          InlineKeyboardButton.WithCallbackData("❌ Отказаться", CallbackData.Task.Cancel(task.Id))
        ]
      ]),
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleCompleteTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Complete task
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

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🎉 Задача выполнена!\n\n⭐ Очки начислены!",
      cancellationToken: cancellationToken);
  }

  private async Task HandleCancelTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Cancel task
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

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Вы отказались от задачи.\n\nЗадача снова доступна для всех участников семьи.",
      cancellationToken: cancellationToken);
  }

  private async Task RequestDueDateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.Data.InternalState = "awaiting_due_date";
    var dueDateKeyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
    {
      ResizeKeyboard = true
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📅 Введите срок выполнения задачи в днях:\n\n" +
      "0 - сегодня\n" +
      "1 - завтра\n" +
      "7 - через неделю\n" +
      "30 - через месяц\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
      cancellationToken: cancellationToken);

    if (dueDateKeyboard != null)
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: dueDateKeyboard,
        cancellationToken: cancellationToken);
  }

  private async Task RequestScheduleAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.Data.InternalState = "awaiting_schedule";
    var scheduleKeyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
    {
      ResizeKeyboard = true
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🔄 Введите расписание задачи в формате Quartz Cron:\n\n" +
      BotMessages.Messages.CronExamples +
      "\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
      ParseMode.Markdown,
      cancellationToken: cancellationToken);

    if (scheduleKeyboard != null)
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: scheduleKeyboard,
        cancellationToken: cancellationToken);
  }
}
