using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateEditHandler(
  ILogger<TemplateEditHandler> logger,
  IMediator mediator) : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateEditTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditTitle),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, title, null, null, null, null, null, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditPoints);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Количество очков должно быть числом. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditPoints),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, new TaskPoints(points), null, null,
      null, null, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditScheduleTimeInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string timeText,
    CancellationToken cancellationToken)
  {
    if (!TimeOnly.TryParse(timeText, out var time))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditScheduleTime);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Неверный формат времени. Используйте формат HH:mm (например, 09:00):",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditScheduleTime),
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
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    if (scheduleType == "manual")
    {
      // For manual schedule type, we don't need time - ask for DueDuration
      session.State = ConversationState.AwaitingTemplateEditDueDuration;
      var dueDurationKeyboard =
        StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.EnterDueDuration +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
        replyMarkup: dueDurationKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "weekly")
    {
      session.State = ConversationState.AwaitingTemplateEditScheduleWeekday;
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.ChooseWeekday +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditScheduleWeekday),
        replyMarkup: weekdayKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "monthly")
    {
      session.State = ConversationState.AwaitingTemplateEditScheduleMonthDay;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditScheduleMonthDay);
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.EnterMonthDay +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditScheduleMonthDay),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      // For daily, workdays, weekends - ask for DueDuration
      session.State = ConversationState.AwaitingTemplateEditDueDuration;
      var dueDurationKeyboard =
        StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.EnterDueDuration +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
        replyMarkup: dueDurationKeyboard,
        cancellationToken: cancellationToken);
    }
  }

  public async Task HandleTemplateEditScheduleMonthDayInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dayText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dayText, out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditScheduleMonthDay);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ День месяца должен быть числом от 1 до 31. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditScheduleMonthDay),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["scheduleDayOfMonth"] = dayOfMonth;

    // Ask for DueDuration
    session.State = ConversationState.AwaitingTemplateEditDueDuration;
    var dueDurationKeyboard =
      StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterDueDuration +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
      replyMarkup: dueDurationKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task UpdateTemplateScheduleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId) ||
        !TryGetSessionData<string>(session, "scheduleType", out var scheduleTypeStr))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
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
        "❌ Ошибка. Попробуйте снова.",
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
    TryGetSessionData<TimeSpan>(session, "dueDuration", out var dueDuration);

    var updateCommand = new UpdateTaskTemplateCommand(
      templateId,
      familyId,
      null,
      null,
      scheduleType,
      scheduleTime,
      scheduleDayOfWeek == default ? null : scheduleDayOfWeek,
      scheduleDayOfMonth == default ? null : scheduleDayOfMonth,
      dueDuration == default ? null : dueDuration);

    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditDueDurationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDurationText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dueDurationText, out var dueDurationHours) || dueDurationHours < 0 || dueDurationHours > 24)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Срок выполнения должен быть числом от 0 до 24 часов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var dueDuration = TimeSpan.FromHours(dueDurationHours);

    // Check if we're in schedule edit flow (scheduleType is set in session)
    if (TryGetSessionData<string>(session, "scheduleType", out _))
    {
      // We're editing schedule, store dueDuration and call UpdateTemplateScheduleAsync
      session.Data["dueDuration"] = dueDuration;
      await UpdateTemplateScheduleAsync(botClient, message, session, cancellationToken);
    }
    else
    {
      // We're only editing DueDuration field
      var updateCommand =
        new UpdateTaskTemplateCommand(templateId, familyId, null, null, null, null, null, null, dueDuration);
      var result = await Mediator.Send(updateCommand, cancellationToken);

      if (!result.IsSuccess)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
          cancellationToken);
        return;
      }

      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Templates.TemplateUpdated,
        replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
        cancellationToken: cancellationToken);
    }
  }
}
