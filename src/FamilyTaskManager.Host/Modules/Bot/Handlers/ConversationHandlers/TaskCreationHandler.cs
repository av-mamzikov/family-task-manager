using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TaskCreationHandler(
  ILogger<TaskCreationHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTaskTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название задачи должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskTitle),
        keyboard,
        cancellationToken);
      return;
    }

    // Store title and move to points input
    session.Data.Title = title;
    session.State = ConversationState.AwaitingTaskPoints;

    var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⭐ Выберите сложность задачи:",
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || !TaskPoints.IsValidValue(points))
    {
      var keyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Пожалуйста, выберите сложность с помощью кнопок:",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
      return;
    }

    // Store points and show Spot selection
    session.Data.Points = points;
    session.State = ConversationState.AwaitingTaskSpotSelection;

    // Get family Spots
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var SpotsResult = await Mediator.Send(getSpotsQuery, cancellationToken);

    if (!SpotsResult.IsSuccess || !SpotsResult.Value.Any())
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        BotConstants.Errors.NoSpots,
        cancellationToken);
      return;
    }

    var buttons = SpotsResult.Value.Select(p =>
    {
      var SpotEmoji = p.Type switch
      {
        SpotType.Cat => "🐱",
        SpotType.Dog => "🐶",
        SpotType.Hamster => "🐹",
        _ => "🐾"
      };
      return new[] { InlineKeyboardButton.WithCallbackData($"{SpotEmoji} {p.Name}", $"taskSpot_{p.Id}") };
    }).ToArray();

    var SpotSelectionKeyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "🐾 Выберите спота, к которому относится задача:",
      replyMarkup: SpotSelectionKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskDueDateInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDateText,
    CancellationToken cancellationToken)
  {
    // Try to parse the date
    if (!int.TryParse(dueDateText, out var days) || days < 0 || days > 365)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskDueDate);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Введите количество дней (от 0 до 365). Например: 1 (завтра), 7 (через неделю):",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskDueDate),
        keyboard,
        cancellationToken);
      return;
    }

    var dueAt = DateTime.UtcNow.AddDays(days);

    // Get all required data from session
    if (session.CurrentFamilyId == null ||
        session.Data.SpotId == null ||
        session.Data.Title == null ||
        session.Data.Points == null)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    // Create one-time task (user id not tracked here anymore)
    var taskPoints = new TaskPoints(session.Data.Points.Value);
    var createTaskCommand =
      new CreateTaskCommand(session.CurrentFamilyId.Value, session.Data.SpotId.Value, session.Data.Title,
        taskPoints, dueAt, session.UserId);
    var result = await Mediator.Send(createTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Задача \"{session.Data.Title}\" успешно создана!\n\n" +
      $"💯 Очки: {taskPoints.ToStars()}\n" +
      $"📎 Срок: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      BotConstants.Messages.TaskAvailableToAll,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  public async Task HandleTaskScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    // Validate schedule (basic check)
    if (string.IsNullOrWhiteSpace(schedule))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    // Get all required data from session
    if (session.CurrentFamilyId == null ||
        session.Data.SpotId == null ||
        session.Data.Title == null ||
        session.Data.Points == null)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    // Parse schedule
    var parseResult = ScheduleParser.Parse(schedule);
    if (!parseResult.IsSuccess)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ {parseResult.Errors.FirstOrDefault()}",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    var (scheduleType, scheduleTime, scheduleDayOfWeek, scheduleDayOfMonth) = parseResult.Value;

    // Create periodic task template
    var taskPoints = new TaskPoints(session.Data.Points.Value);
    var createTemplateCommand =
      new CreateTaskTemplateCommand(session.CurrentFamilyId.Value, session.Data.SpotId.Value,
        session.Data.Title, taskPoints, scheduleType, scheduleTime,
        scheduleDayOfWeek,
        scheduleDayOfMonth, TimeSpan.FromHours(12), session.UserId);
    var result = await Mediator.Send(createTemplateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var scheduleText = ScheduleFormatter.Format(scheduleType, scheduleTime, scheduleDayOfWeek, scheduleDayOfMonth);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Периодическая задача \"{session.Data.Title}\" успешно создана!\n\n" +
      $"💯 Очки: {taskPoints.ToStars()}\n" +
      $"🔄 Расписание: {scheduleText}\n\n" +
      BotConstants.Messages.ScheduledTask,
      cancellationToken: cancellationToken);
    session.ClearState();
  }
}
