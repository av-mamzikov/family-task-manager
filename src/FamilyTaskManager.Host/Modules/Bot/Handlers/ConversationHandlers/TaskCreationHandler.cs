using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
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
  : BaseConversationHandler(logger, mediator), IConversationHandler
{
  private const string StateAwaitingTitle = "awaiting_title";
  private const string StateAwaitingPoints = "awaiting_points";
  private const string StateAwaitingDueDate = "awaiting_due_date";
  private const string StateAwaitingSchedule = "awaiting_schedule";

  public async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var text = message.Text;
    if (string.IsNullOrWhiteSpace(text))
      return;

    if (text is "❌ Отменить" or "/cancel" or "⬅️ Назад")
      return;

    await (session.Data.InternalState switch
    {
      StateAwaitingTitle => HandleTaskTitleInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingPoints => HandleTaskPointsInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingDueDate => HandleTaskDueDateInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingSchedule => HandleTaskScheduleInputAsync(botClient, message, session, text, cancellationToken),
      _ => Task.CompletedTask
    });
  }

  public async Task HandleCancelAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Создание задачи отменено.",
      replyMarkup: new ReplyKeyboardRemove(),
      cancellationToken: cancellationToken);

    await sendMainMenuAction();
    session.ClearState();
  }

  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    var currentState = session.Data.InternalState;

    var previousState = currentState switch
    {
      StateAwaitingPoints => StateAwaitingTitle,
      StateAwaitingDueDate => StateAwaitingPoints,
      StateAwaitingSchedule => StateAwaitingPoints,
      _ => null
    };

    if (previousState == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "⬅️ Возврат отменён.",
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
      await sendMainMenuAction();
      session.ClearState();
      return;
    }

    session.Data.InternalState = previousState;

    var keyboard = GetKeyboardForState(previousState);
    var messageText = GetMessageForState(previousState);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2 || callbackParts[0] != "points")
      return;

    var selection = callbackParts[1];

    if (selection == "back")
    {
      await HandleBackFromPointsAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    if (!int.TryParse(selection, out var points) || !TaskPoints.IsValidValue(points))
      return;

    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

    var fakeMessage = new Message
    {
      Chat = new() { Id = chatId },
      MessageId = messageId
    };

    await HandleTaskPointsInputAsync(botClient, fakeMessage, session, points.ToString(), cancellationToken);
  }

  private async Task HandleTaskTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название задачи должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        $"\n\n💡 Введите название задачи ({TaskTitle.MinLength}-{TaskTitle.MaxLength} символов)",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.Title = title;
    session.Data.InternalState = StateAwaitingPoints;

    var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⭐ Выберите сложность задачи:",
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskPointsInputAsync(
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

    session.Data.Points = points;

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
        BotMessages.Errors.NoSpots,
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

  private async Task HandleTaskDueDateInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDateText,
    CancellationToken cancellationToken)
  {
    // Try to parse the date
    if (!int.TryParse(dueDateText, out var days) || days < 0 || days > 365)
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Введите количество дней (от 0 до 365). Например: 1 (завтра), 7 (через неделю):",
        "\n\n💡 Введите срок в днях (0-365)\n• ⬅️ Назад - К выбору спота",
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
      BotMessages.Messages.TaskAvailableToAll,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private async Task HandleTaskScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    // Validate schedule (basic check)
    if (string.IsNullOrWhiteSpace(schedule))
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        "\n\n💡 Введите расписание в формате Cron\n• ⬅️ Назад - К выбору спота",
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
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ {parseResult.Errors.FirstOrDefault()}",
        "\n\n💡 Введите расписание в формате Cron\n• ⬅️ Назад - К выбору спота",
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
      BotMessages.Messages.ScheduledTask,
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private static IReplyMarkup GetKeyboardForState(string state) =>
    state switch
    {
      StateAwaitingTitle => new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
        { ResizeKeyboard = true },
      StateAwaitingPoints => TaskPointsHelper.GetPointsSelectionKeyboard(),
      _ => new ReplyKeyboardRemove()
    };

  private static string GetMessageForState(string state) =>
    state switch
    {
      StateAwaitingTitle => $"📝 Введите название задачи (от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов):",
      StateAwaitingPoints => "⭐ Выберите сложность задачи:",
      _ => "⬅️ Возврат к предыдущему шагу."
    };

  private async Task HandleBackFromPointsAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
    session.Data.InternalState = StateAwaitingTitle;
    var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
    {
      ResizeKeyboard = true
    };
    await botClient.SendTextMessageAsync(
      chatId,
      $"📝 Введите название задачи (от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов):\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
