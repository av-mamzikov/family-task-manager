using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateCreationHandler(
  ILogger<TemplateCreationHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger, mediator), IConversationHandler
{
  private const string StateAwaitingTitle = "awaiting_title";
  private const string StateAwaitingPoints = "awaiting_points";
  private const string StateAwaitingScheduleTime = "awaiting_schedule_time";
  private const string StateAwaitingScheduleMonthDay = "awaiting_schedule_month_day";
  private const string StateAwaitingDueDuration = "awaiting_due_duration";

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
      StateAwaitingTitle => HandleTemplateTitleInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingPoints => HandleTemplatePointsInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingScheduleTime => HandleTemplateScheduleTimeInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingScheduleMonthDay => HandleTemplateScheduleMonthDayInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingDueDuration => HandleTemplateDueDurationInputAsync(botClient, message, session, text,
        cancellationToken),
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
      "❌ Создание шаблона отменено.",
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
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⬅️ Возврат в шаблонах пока не поддерживается.",
      replyMarkup: new ReplyKeyboardRemove(),
      cancellationToken: cancellationToken);
    await sendMainMenuAction();
    session.ClearState();
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
    if (callbackParts.Length < 2)
      return;

    var action = callbackParts[0];

    if (action == "points")
      await HandlePointsCallbackAsync(botClient, chatId, messageId, callbackParts, session, cancellationToken);
    else if (action == "schedule")
      await HandleScheduleCallbackAsync(botClient, chatId, messageId, callbackParts, session, cancellationToken);
  }

  private async Task HandleTemplateTitleInputAsync(
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
        $"❌ Название шаблона должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        $"\n\n💡 Введите название шаблона ({TaskTitle.MinLength}-{TaskTitle.MaxLength} символов)",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.Title = title;
    session.Data.InternalState = StateAwaitingPoints;

    var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterTemplatePoints,
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplatePointsInputAsync(
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

    var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.ChooseScheduleType + "\n\n💡 Выберите тип расписания с помощью кнопок",
      replyMarkup: scheduleTypeKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateScheduleTimeInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string timeText,
    CancellationToken cancellationToken)
  {
    if (!TimeOnly.TryParse(timeText, out var time))
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Неверный формат времени. Используйте формат HH:mm (например, 09:00):",
        "\n\n💡 Введите время (HH:mm)\n• ⬅️ Назад - К типу расписания",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.ScheduleTime = time;

    // Check if we need additional input based on schedule type
    var scheduleType = session.Data.ScheduleType;
    if (string.IsNullOrWhiteSpace(scheduleType))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать шаблон заново.",
        cancellationToken);
      return;
    }

    if (scheduleType == "weekly")
    {
      session.Data.InternalState = "awaiting_schedule_weekday";
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.ChooseWeekday + "\n\n💡 Используйте кнопки для выбора.",
        replyMarkup: weekdayKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "monthly")
    {
      session.Data.InternalState = StateAwaitingScheduleMonthDay;
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.EnterMonthDay + "\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      session.Data.InternalState = StateAwaitingDueDuration;
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.EnterDueDuration,
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandleTemplateScheduleMonthDayInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dayText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dayText, out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ День месяца должен быть числом от 1 до 31. Попробуйте снова:",
        "\n\n💡 Введите день месяца (1-31)\n• ⬅️ Назад - К типу расписания",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.ScheduleDayOfMonth = dayOfMonth;

    session.Data.InternalState = StateAwaitingDueDuration;
    var dueDurationKeyboard =
      new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterDueDuration,
      replyMarkup: dueDurationKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateDueDurationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDurationText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dueDurationText, out var dueDurationHours) || dueDurationHours < 0 || dueDurationHours > 24)
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Срок выполнения должен быть числом от 0 до 24 часов. Попробуйте снова:",
        "\n\n💡 Введите срок в часах (0-24)\n• ⬅️ Назад - К расписанию",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.DueDuration = TimeSpan.FromHours(dueDurationHours);
    await CreateTemplateAsync(botClient, message, session, cancellationToken);
  }

  private async Task CreateTemplateAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get all required data from session (strongly-typed)
    var data = session.Data;

    if (session.CurrentFamilyId is not { } familyId ||
        data.SpotId is not { } SpotId ||
        string.IsNullOrWhiteSpace(data.Title) ||
        data.Points is not { } points ||
        string.IsNullOrWhiteSpace(data.ScheduleType) ||
        data.DueDuration is not { } dueDuration)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать шаблон заново.",
        cancellationToken);
      return;
    }

    var title = data.Title;
    var scheduleTypeStr = data.ScheduleType;

    // For Manual schedule type, time is not required
    var scheduleTime = TimeOnly.MinValue;
    if (scheduleTypeStr != "manual")
    {
      if (data.ScheduleTime is null)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          "❌ Ошибка. Попробуйте создать шаблон заново.",
          cancellationToken);
        return;
      }

      scheduleTime = data.ScheduleTime.Value;
    }

    // Map schedule type string to ScheduleType
    var scheduleType = scheduleTypeStr switch
    {
      "daily" => ScheduleType.Daily,
      "workdays" => ScheduleType.Workdays,
      "weekends" => ScheduleType.Weekends,
      "weekly" => ScheduleType.Weekly,
      "monthly" => ScheduleType.Monthly,
      "manual" => ScheduleType.Manual,
      _ => null
    };

    if (scheduleType == null)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Неизвестный тип расписания.",
        cancellationToken);
      return;
    }

    // Get optional schedule parameters
    var scheduleDayOfWeek = data.ScheduleDayOfWeek;
    var scheduleDayOfMonth = data.ScheduleDayOfMonth;

    // Create template
    var createTemplateCommand = new CreateTaskTemplateCommand(
      familyId,
      SpotId,
      title,
      new(points),
      scheduleType,
      scheduleTime,
      scheduleDayOfWeek == default ? null : scheduleDayOfWeek,
      scheduleDayOfMonth == default ? null : scheduleDayOfMonth,
      dueDuration,
      session.UserId);

    var result = await Mediator.Send(createTemplateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания шаблона: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    // Build schedule description
    var scheduleDescription = BuildScheduleDescription(scheduleTypeStr, scheduleTime, scheduleDayOfWeek,
      scheduleDayOfMonth);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Шаблон \"{title}\" успешно создан!\n\n" +
      $"💯 Очки: {TaskPointsHelper.ToStars(points)}\n" +
      $"🔄 Расписание: {scheduleDescription}\n\n" +
      BotMessages.Messages.ScheduledTask,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private static string BuildScheduleDescription(
    string scheduleType,
    TimeOnly time,
    DayOfWeek? dayOfWeek,
    int? dayOfMonth)
  {
    var typeName = ScheduleKeyboardHelper.GetScheduleTypeName(scheduleType);

    return scheduleType switch
    {
      "manual" => typeName,
      "weekly" when dayOfWeek.HasValue =>
        $"{typeName}, {ScheduleKeyboardHelper.GetWeekdayName(dayOfWeek.Value)} в {time:HH:mm}",
      "monthly" when dayOfMonth.HasValue => $"{typeName}, {dayOfMonth}-го числа в {time:HH:mm}",
      _ => $"{typeName} в {time:HH:mm}"
    };
  }

  private async Task HandlePointsCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var selection = callbackParts[1];

    if (selection == "back")
    {
      await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
      session.Data.InternalState = StateAwaitingTitle;
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await botClient.SendTextMessageAsync(
        chatId,
        $"📝 Введите название шаблона задачи (от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов):\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
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

    await HandleTemplatePointsInputAsync(botClient, fakeMessage, session, points.ToString(), cancellationToken);
  }

  private async Task HandleScheduleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2)
      return;

    var scheduleAction = callbackParts[1];

    if (scheduleAction == "type" && callbackParts.Length >= 3)
    {
      var scheduleType = callbackParts[2];
      session.Data.ScheduleType = scheduleType;

      if (scheduleType == "manual")
      {
        session.Data.InternalState = StateAwaitingDueDuration;
        var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
        {
          ResizeKeyboard = true
        };
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.EnterDueDuration,
          cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(
          chatId,
          "Используйте кнопки ниже:",
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
      }
      else
      {
        session.Data.InternalState = StateAwaitingScheduleTime;
        var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
        {
          ResizeKeyboard = true
        };
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "🕐 Введите время выполнения в формате HH:mm (например, 09:00):",
          cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(
          chatId,
          "Используйте кнопки ниже:",
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
      }
    }
    else if (scheduleAction == "weekday" && callbackParts.Length >= 3)
      if (Enum.TryParse<DayOfWeek>(callbackParts[2], out var dayOfWeek))
      {
        session.Data.ScheduleDayOfWeek = dayOfWeek;
        session.Data.InternalState = StateAwaitingDueDuration;
        var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
        {
          ResizeKeyboard = true
        };
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.EnterDueDuration,
          cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(
          chatId,
          "Используйте кнопки ниже:",
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
      }
  }
}
