using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateEditHandler(
  ILogger<TemplateEditHandler> logger,
  IMediator mediator,
  TemplateCommandHandler templateCommandHandler) : BaseConversationHandler(logger, mediator), IConversationHandler
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
      StateAwaitingTitle => HandleTemplateEditTitleInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingPoints => HandleTemplateEditPointsInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingScheduleTime => HandleTemplateEditScheduleTimeInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingScheduleMonthDay => HandleTemplateEditScheduleMonthDayInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingDueDuration => HandleTemplateEditDueDurationInputAsync(botClient, message, session, text,
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
      "❌ Редактирование шаблона отменено.",
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
      "⬅️ Возврат при редактировании пока не поддерживается.",
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
    {
      var selection = callbackParts[1];

      if (selection == "back" && session.Data.TemplateId is Guid templateId)
      {
        session.ClearState();
        await templateCommandHandler.HandleEditTemplateAsync(
          botClient,
          chatId,
          messageId,
          templateId,
          session,
          cancellationToken);
        return;
      }

      if (!int.TryParse(selection, out var points) || points < 1 || points > 3)
        return;

      var fakeMessage = new Message
      {
        Chat = new() { Id = chatId },
        MessageId = messageId
      };

      await HandleTemplateEditPointsInputAsync(botClient, fakeMessage, session, points.ToString(), cancellationToken);
    }
    else if (action == "schedule")
      await HandleScheduleCallbackForEditAsync(botClient, chatId, messageId, callbackParts, session, cancellationToken);
  }

  private async Task HandleTemplateEditTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название шаблона должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        "\n\n💡 Введите новое название\n• ⬅️ Назад - Отменить редактирование",
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

  private async Task HandleTemplateEditPointsInputAsync(
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

  private async Task HandleTemplateEditScheduleTimeInputAsync(
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
        "\n\n💡 Введите новое время\n• ⬅️ Назад - Отменить редактирование",
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
    else if (scheduleType == "weekly")
    {
      session.Data.InternalState = "awaiting_schedule_weekday";
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.ChooseWeekday + "\n\n💡 Выберите новый день недели с помощью кнопок",
        replyMarkup: weekdayKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == "monthly")
    {
      session.Data.InternalState = StateAwaitingScheduleMonthDay;
      var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") } })
      {
        ResizeKeyboard = true
      };
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.EnterMonthDay + "\n\n💡 Введите новый день месяца\n• ⬅️ Назад - Отменить редактирование",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
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
  }

  private async Task HandleTemplateEditScheduleMonthDayInputAsync(
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
        "\n\n💡 Введите новый день месяца\n• ⬅️ Назад - Отменить редактирование",
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

  private async Task UpdateTemplateScheduleAsync(
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

  private async Task HandleTemplateEditDueDurationInputAsync(
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
        "\n\n💡 Введите новый срок в часах (0-24)\n• ⬅️ Назад - Отменить редактирование",
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

  private async Task HandleScheduleCallbackForEditAsync(
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
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.EnterDueDuration,
          cancellationToken: cancellationToken);
      }
      else
      {
        session.Data.InternalState = StateAwaitingScheduleTime;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "🕐 Введите время выполнения в формате HH:mm (например, 09:00):",
          cancellationToken: cancellationToken);
      }
    }
    else if (scheduleAction == "weekday" && callbackParts.Length >= 3)
      if (Enum.TryParse<DayOfWeek>(callbackParts[2], out var dayOfWeek))
      {
        session.Data.ScheduleDayOfWeek = dayOfWeek;
        session.Data.InternalState = StateAwaitingDueDuration;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.EnterDueDuration,
          cancellationToken: cancellationToken);
      }
  }
}
