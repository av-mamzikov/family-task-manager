using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateCreationHandler(
  ILogger<TemplateCreationHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateTitle),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["title"] = title;
    session.State = ConversationState.AwaitingTemplatePoints;

    var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterTemplatePoints,
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplatePointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 3)
    {
      var keyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Пожалуйста, выберите сложность с помощью кнопок:",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
      return;
    }

    session.Data["points"] = points;
    session.State = ConversationState.AwaitingTemplateScheduleType;

    var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.ChooseScheduleType +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateScheduleType),
      replyMarkup: scheduleTypeKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateScheduleTimeInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string timeText,
    CancellationToken cancellationToken)
  {
    if (!TimeOnly.TryParse(timeText, out var time))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateScheduleTime);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Неверный формат времени. Используйте формат HH:mm (например, 09:00):",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateScheduleTime),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["scheduleTime"] = time;

    // Check if we need additional input based on schedule type
    if (!TryGetSessionData<string>(session, "scheduleType", out var scheduleType))
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
      session.State = ConversationState.AwaitingTemplateScheduleWeekday;
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.ChooseWeekday +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateScheduleWeekday),
        replyMarkup: weekdayKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "monthly")
    {
      session.State = ConversationState.AwaitingTemplateScheduleMonthDay;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateScheduleMonthDay);
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.EnterMonthDay +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateScheduleMonthDay),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      // For daily, workdays, weekends - ask for DueDuration
      session.State = ConversationState.AwaitingTemplateDueDuration;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.EnterDueDuration +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateDueDuration),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
  }

  public async Task HandleTemplateScheduleMonthDayInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dayText,
    CancellationToken cancellationToken)
  {
    IReplyMarkup? keyboard;
    if (!int.TryParse(dayText, out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
    {
      keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateScheduleMonthDay);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ День месяца должен быть числом от 1 до 31. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateScheduleMonthDay),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["scheduleDayOfMonth"] = dayOfMonth;

    // Ask for DueDuration
    session.State = ConversationState.AwaitingTemplateDueDuration;
    keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterDueDuration +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateDueDuration),
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateDueDurationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDurationText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dueDurationText, out var dueDurationHours) || dueDurationHours < 0 || dueDurationHours > 24)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Срок выполнения должен быть числом от 0 до 24 часов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateDueDuration),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["dueDuration"] = TimeSpan.FromHours(dueDurationHours);
    await CreateTemplateAsync(botClient, message, session, cancellationToken);
  }

  public async Task CreateTemplateAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get all required data from session
    if (!TryGetSessionData<Guid>(session, "familyId", out var familyId) ||
        !TryGetSessionData<Guid>(session, "petId", out var petId) ||
        !TryGetSessionData<string>(session, "title", out var title) ||
        !TryGetSessionData<int>(session, "points", out var points) ||
        !TryGetSessionData<string>(session, "scheduleType", out var scheduleTypeStr) ||
        !TryGetSessionData<TimeSpan>(session, "dueDuration", out var dueDuration))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать шаблон заново.",
        cancellationToken);
      return;
    }

    // For Manual schedule type, time is not required
    var scheduleTime = TimeOnly.MinValue;
    if (scheduleTypeStr != "manual" && !TryGetSessionData(session, "scheduleTime", out scheduleTime))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать шаблон заново.",
        cancellationToken);
      return;
    }

    // Get user ID
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken);
      return;
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
    TryGetSessionData<DayOfWeek>(session, "scheduleDayOfWeek", out var scheduleDayOfWeek);
    TryGetSessionData<int>(session, "scheduleDayOfMonth", out var scheduleDayOfMonth);

    // Create template
    var createTemplateCommand = new CreateTaskTemplateCommand(
      familyId,
      petId,
      title,
      new TaskPoints(points),
      scheduleType,
      scheduleTime,
      scheduleDayOfWeek == default ? null : scheduleDayOfWeek,
      scheduleDayOfMonth == default ? null : scheduleDayOfMonth,
      dueDuration,
      userResult.Value);

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

    session.ClearState();

    // Build schedule description
    var scheduleDescription = BuildScheduleDescription(scheduleTypeStr, scheduleTime, scheduleDayOfWeek,
      scheduleDayOfMonth);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"{BotConstants.Templates.TemplateCreated}\n\n" +
      $"✅ Шаблон \"{title}\" успешно создан!\n\n" +
      $"💯 Очки: {TaskPointsHelper.ToStars(points)}\n" +
      $"🔄 Расписание: {scheduleDescription}\n\n" +
      BotConstants.Messages.ScheduledTask,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
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
}
