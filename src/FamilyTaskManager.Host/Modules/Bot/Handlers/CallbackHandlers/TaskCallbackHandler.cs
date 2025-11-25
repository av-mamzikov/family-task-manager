using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class TaskCallbackHandler(
  ILogger<TaskCallbackHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
{
  public async Task StartCreateTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Сначала выберите активную семью", cancellationToken);
      return;
    }

    // Ask user to select task type
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📝 Разовая задача", "select_tasktype_onetime") },
      new[] { InlineKeyboardButton.WithCallbackData("🔄 Периодическая задача", "select_tasktype_recurring") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📋 *Создание задачи*\n\nВыберите тип задачи:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string taskType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Store task type in session
    session.SetState(ConversationState.AwaitingTaskTitle,
      new Dictionary<string, object> { ["taskType"] = taskType, ["familyId"] = session.CurrentFamilyId! });

    var taskTypeText = taskType == "onetime" ? "разовую" : "периодическую";
    var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskTitle);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"📝 Создание {taskTypeText} задачи\n\nВведите название задачи (от 3 до 100 символов):" +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskTitle),
      cancellationToken: cancellationToken);

    // Send keyboard in a separate message
    if (keyboard != null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
  }

  public async Task HandleTaskPetSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2)
    {
      return;
    }

    if (!Guid.TryParse(parts[1], out var petId))
    {
      return;
    }

    // Store pet ID in session
    session.Data["petId"] = petId;

    // Check task type to determine next step
    if (!TryGetSessionData<string>(session, "taskType", out var taskType))
    {
      session.ClearState();
      await SendErrorAsync(botClient, chatId, "❌ Ошибка. Попробуйте создать задачу заново.", cancellationToken);
      return;
    }

    if (taskType == "onetime")
    {
      await RequestDueDateAsync(botClient, chatId, messageId, session, cancellationToken);
    }
    else
    {
      await RequestScheduleAsync(botClient, chatId, messageId, session, cancellationToken);
    }
  }

  public async Task HandleTaskActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3)
    {
      return;
    }

    var taskAction = parts[1];
    var taskIdStr = parts[2];

    if (!Guid.TryParse(taskIdStr, out var taskId))
    {
      return;
    }

    switch (taskAction)
    {
      case "take":
        await HandleTakeTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;

      case "complete":
        await HandleCompleteTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
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
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    // Take task
    var takeTaskCommand = new TakeTaskCommand(taskId, userId.Value);
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

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Задача взята в работу!\n\nТеперь вы можете её выполнить.",
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
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    // Complete task
    var completeTaskCommand = new CompleteTaskCommand(taskId, userId.Value);
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

  private async Task RequestDueDateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // For one-time tasks, ask for due date
    session.State = ConversationState.AwaitingTaskDueDate;
    var dueDateKeyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskDueDate);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📅 Введите срок выполнения задачи в днях:\n\n" +
      "0 - сегодня\n" +
      "1 - завтра\n" +
      "7 - через неделю\n" +
      "30 - через месяц" +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskDueDate),
      cancellationToken: cancellationToken);

    if (dueDateKeyboard != null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: dueDateKeyboard,
        cancellationToken: cancellationToken);
    }
  }

  private async Task RequestScheduleAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // For recurring tasks, ask for schedule
    session.State = ConversationState.AwaitingTaskSchedule;
    var scheduleKeyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskSchedule);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🔄 Введите расписание задачи в формате Quartz Cron:\n\n" +
      BotConstants.Messages.CronExamples +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskSchedule),
      ParseMode.Markdown,
      cancellationToken: cancellationToken);

    if (scheduleKeyboard != null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: scheduleKeyboard,
        cancellationToken: cancellationToken);
    }
  }
}
