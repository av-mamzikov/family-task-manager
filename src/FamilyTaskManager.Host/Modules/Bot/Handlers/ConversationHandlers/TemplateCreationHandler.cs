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
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название шаблона должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateTitle),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.Title = title;
    session.State = ConversationState.AwaitingTemplatePoints;

    var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterTemplatePoints,
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
    session.State = ConversationState.AwaitingTemplateScheduleType;

    var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.ChooseScheduleType +
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
      session.State = ConversationState.AwaitingTemplateScheduleWeekday;
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.ChooseWeekday +
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
        BotMessages.Templates.EnterMonthDay +
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
        BotMessages.Templates.EnterDueDuration,
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

    session.Data.ScheduleDayOfMonth = dayOfMonth;

    // Ask for DueDuration
    session.State = ConversationState.AwaitingTemplateDueDuration;
    keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterDueDuration,
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

    session.Data.DueDuration = TimeSpan.FromHours(dueDurationHours);
    await CreateTemplateAsync(botClient, message, session, cancellationToken);
  }

  public async Task CreateTemplateAsync(
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
}
