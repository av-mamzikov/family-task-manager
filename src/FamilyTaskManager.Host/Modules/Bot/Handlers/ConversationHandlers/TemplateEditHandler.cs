using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateEditHandler(
  ILogger<TemplateEditHandler> logger,
  IMediator mediator,
  TemplateCommandHandler templateCommandHandler) : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateEditTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название шаблона должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditTitle),
        keyboard,
        cancellationToken);
      return;
    }

    if (session.Data.TemplateId is not { } templateId ||
        session.CurrentFamilyId is not { } familyId)
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

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  public async Task HandleTemplateEditPointsInputAsync(
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

    if (session.Data.TemplateId is not { } templateId ||
        session.CurrentFamilyId is not { } familyId)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, new(points), null, null,
      null, null, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        message.Chat.Id,
        message.MessageId,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    // Return to template edit screen
    await templateCommandHandler.HandleEditTemplateAsync(
      botClient,
      message.Chat.Id,
      message.MessageId,
      templateId,
      session,
      cancellationToken);
    session.ClearState();
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

    session.Data.ScheduleTime = time;

    // Check if we need additional input based on schedule type
    var scheduleType = session.Data.ScheduleType;
    if (string.IsNullOrWhiteSpace(scheduleType))
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
        BotMessages.Templates.EnterDueDuration,
        replyMarkup: dueDurationKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "weekly")
    {
      session.State = ConversationState.AwaitingTemplateEditScheduleWeekday;
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.ChooseWeekday +
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
        BotMessages.Templates.EnterMonthDay +
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
        BotMessages.Templates.EnterDueDuration,
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

    session.Data.ScheduleDayOfMonth = dayOfMonth;

    // Ask for DueDuration
    session.State = ConversationState.AwaitingTemplateEditDueDuration;
    var dueDurationKeyboard =
      StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterDueDuration,
      replyMarkup: dueDurationKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task UpdateTemplateScheduleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var data = session.Data;

    if (data.TemplateId is not { } templateId ||
        session.CurrentFamilyId is not { } familyId ||
        string.IsNullOrWhiteSpace(data.ScheduleType))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

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
          "❌ Ошибка. Попробуйте снова.",
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
    var dueDuration = data.DueDuration;

    var updateCommand = new UpdateTaskTemplateCommand(
      templateId,
      familyId,
      null,
      null,
      scheduleType,
      scheduleTime,
      scheduleDayOfWeek == default ? null : scheduleDayOfWeek,
      scheduleDayOfMonth == default ? null : scheduleDayOfMonth,
      dueDuration);

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

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
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

    if (session.Data.TemplateId is not { } templateId ||
        session.CurrentFamilyId is not { } familyId)
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
    if (!string.IsNullOrWhiteSpace(session.Data.ScheduleType))
    {
      // We're editing schedule, store dueDuration and call UpdateTemplateScheduleAsync
      session.Data.DueDuration = dueDuration;
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

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.TemplateUpdated,
        replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
        cancellationToken: cancellationToken);
      session.ClearState();
    }
  }
}
