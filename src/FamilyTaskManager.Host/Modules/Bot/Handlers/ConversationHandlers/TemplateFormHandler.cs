using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateFormHandler(
  ILogger<TemplateFormHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger), IConversationHandler
{
  private const string StateAwaitingTitle = "awaiting_title";
  private const string StateAwaitingPoints = "awaiting_points";
  private const string StateAwaitingScheduleTime = "awaiting_schedule_time";
  private const string StateAwaitingScheduleWeekday = "awaiting_schedule_weekday";
  private const string StateAwaitingScheduleMonthDay = "awaiting_schedule_month_day";
  private const string StateAwaitingDueDuration = "awaiting_due_duration";

  private const string FieldTitle = "title";
  private const string FieldPoints = "points";
  private const string FieldSchedule = "schedule";
  private const string FieldDueDuration = "dueduration";

  private const string ScheduleTypeDaily = "daily";
  private const string ScheduleTypeWorkdays = "workdays";
  private const string ScheduleTypeWeekends = "weekends";
  private const string ScheduleTypeWeekly = "weekly";
  private const string ScheduleTypeMonthly = "monthly";
  private const string ScheduleTypeManual = "manual";

  public async Task HandleMessageAsync(
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
      StateAwaitingTitle => HandleTitleInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingPoints => HandlePointsInputAsync(botClient, message.Chat.Id, message, session, text,
        cancellationToken),
      StateAwaitingScheduleTime => HandleScheduleTimeInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingScheduleMonthDay => HandleScheduleMonthDayInputAsync(botClient, message, session, text,
        cancellationToken),
      StateAwaitingDueDuration => HandleDueDurationInputAsync(botClient, message, session, text,
        cancellationToken),
      _ => Task.CompletedTask
    });
  }

  public async Task HandleCallbackAsync(ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.Create, out EncodedGuid spotId))
      await HandleTemplateCreateForSpotAsync(botClient, chatId, message, spotId.Value, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.Edit, out EncodedGuid editTemplateId))
      await HandleEditTemplateAsync(botClient, chatId, message, editTemplateId.Value, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.EditField, out var templateId,
               out var fieldName))
      await HandleTemplateEditFieldAsync(botClient, chatId, message, templateId.Value, fieldName, session,
        cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.SelectPoints, out var points))
      await HandlePointsSelectionAsync(botClient, chatId, message, session, points, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.SelectScheduleType, out string scheduleType))
      await HandleScheduleTypeSelectionAsync(botClient, chatId, message, session, scheduleType, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateForm.SelectWeekday, out string weekday))
      await HandleWeekdaySelectionAsync(botClient, chatId, message, session, weekday, cancellationToken);
  }

  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      isEdit
        ? "⬅️ Возврат при редактировании пока не поддерживается."
        : "⬅️ Возврат при создании пока не поддерживается.",
      replyMarkup: new ReplyKeyboardRemove(),
      cancellationToken: cancellationToken);
    await sendMainMenuAction();
    session.ClearState();
  }

  private async Task HandleTemplateCreateForSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    session.State = ConversationState.TemplateForm;
    session.Data = new() { SpotId = spotId, TemplateId = null, InternalState = StateAwaitingTitle };

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      BotMessages.Templates.EnterTemplateTitle,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateEditFieldAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid templateId,
    string fieldCode,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    session.State = ConversationState.TemplateForm;
    session.Data.TemplateId = templateId;

    switch (fieldCode)
    {
      case FieldTitle:
        session.Data.InternalState = StateAwaitingTitle;
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          "✏️ Введите новое название шаблона (от 3 до 100 символов):",
          cancellationToken: cancellationToken);
        break;

      case FieldPoints:
        session.Data.InternalState = StateAwaitingPoints;
        var pointsKeyboard =
          TaskPointsHelper.GetPointsSelectionKeyboard(CallbackData.TemplateForm.Edit(templateId));
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          "⭐ Выберите новую сложность задачи:",
          replyMarkup: pointsKeyboard,
          cancellationToken: cancellationToken);
        break;

      case FieldSchedule:
        session.Data.InternalState = StateAwaitingScheduleTime;
        session.Data.ScheduleType = null;
        var scheduleTypeKeyboard =
          ScheduleKeyboardHelper.GetScheduleTypeKeyboard(CallbackData.TemplateForm.Edit(templateId));
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          BotMessages.Templates.ChooseScheduleType +
          "\n\n💡 Используйте кнопки для выбора.",
          replyMarkup: scheduleTypeKeyboard,
          cancellationToken: cancellationToken);
        break;

      case FieldDueDuration:
        session.Data.InternalState = StateAwaitingDueDuration;
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          "⏰ Введите новый срок выполнения в часах (от 0 до 24):",
          cancellationToken: cancellationToken);
        break;

      default:
        await SendErrorAsync(botClient, chatId, "❌ Неизвестное поле", cancellationToken);
        break;
    }
  }

  public async Task HandleEditTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var getTemplateQuery = new GetTaskTemplateByIdQuery(templateId, session.CurrentFamilyId.Value);
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Шаблон не найден",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("✏️ Название",
          CallbackData.TemplateForm.EditField(templateId, FieldTitle))
      ],
      [
        InlineKeyboardButton.WithCallbackData("💯 Очки",
          CallbackData.TemplateForm.EditField(templateId, FieldPoints))
      ],
      [
        InlineKeyboardButton.WithCallbackData("🔄 Расписание",
          CallbackData.TemplateForm.EditField(templateId, FieldSchedule))
      ],
      [
        InlineKeyboardButton.WithCallbackData("⏰ Срок выполнения",
          CallbackData.TemplateForm.EditField(templateId, FieldDueDuration))
      ],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.TemplateBrowsing.View(templateId))]
    ]);

    await botClient.SendOrEditMessageAsync(chatId, message,
      $"✏️ *Редактирование шаблона*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
      $"⏰ Срок выполнения: {template.DueDuration.TotalHours} часов\n\n" +
      "Выберите поле для редактирования:",
      ParseMode.Markdown, keyboard, cancellationToken);
  }

  private async Task HandleTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;

    if (string.IsNullOrWhiteSpace(title) || title.Length < TaskTitle.MinLength || title.Length > TaskTitle.MaxLength)
    {
      var keyboard = new ReplyKeyboardMarkup([[new(isEdit ? "⬅️ Назад" : "❌ Отменить"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ Название шаблона должно содержать от {TaskTitle.MinLength} до {TaskTitle.MaxLength} символов. Попробуйте снова:",
        $"\n\n💡 Введите название шаблона ({TaskTitle.MinLength}-{TaskTitle.MaxLength} символов)" +
        (isEdit ? "\n• ⬅️ Назад - Отменить редактирование" : ""),
        keyboard,
        cancellationToken);
      return;
    }

    if (isEdit)
    {
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

      var updateCommand =
        new UpdateTaskTemplateCommand(templateId, familyId, title, null, null, null, null, null, null);
      var result = await mediator.Send(updateCommand, cancellationToken);

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
    else
    {
      session.Data.Title = title;
      session.Data.InternalState = StateAwaitingPoints;

      var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard("points_back");
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.EnterTemplatePoints,
        replyMarkup: pointsKeyboard,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandlePointsInputAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;

    if (!int.TryParse(pointsText, out var points) || !TaskPoints.IsValidValue(points))
    {
      var keyboard = TaskPointsHelper.GetPointsSelectionKeyboard("points_back");
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Пожалуйста, выберите сложность с помощью кнопок:",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
      return;
    }

    if (isEdit)
    {
      if (session.Data.TemplateId is not { } templateId ||
          session.CurrentFamilyId is not { } familyId)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          chatId,
          session,
          "❌ Ошибка. Попробуйте снова.",
          cancellationToken);
        return;
      }

      var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, new(points), null, null,
        null, null, null);
      var result = await mediator.Send(updateCommand, cancellationToken);

      if (!result.IsSuccess)
      {
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
          cancellationToken: cancellationToken);
        return;
      }

      await HandleEditTemplateAsync(
        botClient,
        chatId,
        message,
        templateId,
        session,
        cancellationToken);
      session.ClearState();
    }
    else
    {
      session.Data.Points = points;

      var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard("schedule_back");
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Templates.ChooseScheduleType + "\n\n💡 Выберите тип расписания с помощью кнопок",
        replyMarkup: scheduleTypeKeyboard,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandleScheduleTimeInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string timeText,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;

    if (!TimeOnly.TryParse(timeText, out var time))
    {
      var keyboard = new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Неверный формат времени. Используйте формат HH:mm (например, 09:00):",
        isEdit
          ? "\n\n💡 Введите новое время\n• ⬅️ Назад - Отменить редактирование"
          : "\n\n💡 Введите время (HH:mm)\n• ⬅️ Назад - К типу расписания",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.ScheduleTime = time;

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

    if (scheduleType == ScheduleTypeWeekly)
    {
      session.Data.InternalState = StateAwaitingScheduleWeekday;
      var weekdayKeyboard = ScheduleKeyboardHelper.GetWeekdayKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.ChooseWeekday +
        (isEdit ? "\n\n💡 Выберите новый день недели с помощью кнопок" : "\n\n💡 Используйте кнопки для выбора."),
        replyMarkup: weekdayKeyboard,
        cancellationToken: cancellationToken);
    }
    else if (scheduleType == ScheduleTypeMonthly)
    {
      session.Data.InternalState = StateAwaitingScheduleMonthDay;
      var keyboard = new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Templates.EnterMonthDay +
        (isEdit
          ? "\n\n💡 Введите новый день месяца\n• ⬅️ Назад - Отменить редактирование"
          : "\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены."),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      session.Data.InternalState = StateAwaitingDueDuration;
      var keyboard = new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
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

  private async Task HandleScheduleMonthDayInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dayText,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;

    if (!int.TryParse(dayText, out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
    {
      var keyboard = new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ День месяца должен быть числом от 1 до 31. Попробуйте снова:",
        isEdit
          ? "\n\n💡 Введите новый день месяца\n• ⬅️ Назад - Отменить редактирование"
          : "\n\n💡 Введите день месяца (1-31)\n• ⬅️ Назад - К типу расписания",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.ScheduleDayOfMonth = dayOfMonth;

    session.Data.InternalState = StateAwaitingDueDuration;
    var dueDurationKeyboard =
      new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Templates.EnterDueDuration,
      replyMarkup: dueDurationKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleDueDurationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDurationText,
    CancellationToken cancellationToken)
  {
    var isEdit = session.Data.TemplateId.HasValue;

    if (!int.TryParse(dueDurationText, out var dueDurationHours) || dueDurationHours < 0 || dueDurationHours > 24)
    {
      var keyboard = new ReplyKeyboardMarkup([[new("⬅️ Назад"), new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Срок выполнения должен быть числом от 0 до 24 часов. Попробуйте снова:",
        isEdit
          ? "\n\n💡 Введите новый срок в часах (0-24)\n• ⬅️ Назад - Отменить редактирование"
          : "\n\n💡 Введите срок в часах (0-24)\n• ⬅️ Назад - К расписанию",
        keyboard,
        cancellationToken);
      return;
    }

    var dueDuration = TimeSpan.FromHours(dueDurationHours);

    if (isEdit)
    {
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

      if (!string.IsNullOrWhiteSpace(session.Data.ScheduleType))
      {
        session.Data.DueDuration = dueDuration;
        await UpdateTemplateScheduleAsync(botClient, message, session, cancellationToken);
      }
      else
      {
        var updateCommand =
          new UpdateTaskTemplateCommand(templateId, familyId, null, null, null, null, null, null, dueDuration);
        var result = await mediator.Send(updateCommand, cancellationToken);

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
    else
    {
      session.Data.DueDuration = dueDuration;
      await CreateTemplateAsync(botClient, message, session, cancellationToken);
    }
  }

  private async Task CreateTemplateAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var data = session.Data;

    if (session.CurrentFamilyId is not { } familyId ||
        data.SpotId is not { } spotId ||
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

    var scheduleTime = TimeOnly.MinValue;
    if (scheduleTypeStr != ScheduleTypeManual)
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

    var scheduleType = scheduleTypeStr switch
    {
      ScheduleTypeDaily => ScheduleType.Daily,
      ScheduleTypeWorkdays => ScheduleType.Workdays,
      ScheduleTypeWeekends => ScheduleType.Weekends,
      ScheduleTypeWeekly => ScheduleType.Weekly,
      ScheduleTypeMonthly => ScheduleType.Monthly,
      ScheduleTypeManual => ScheduleType.Manual,
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

    var scheduleDayOfWeek = data.ScheduleDayOfWeek;
    var scheduleDayOfMonth = data.ScheduleDayOfMonth;

    var createTemplateCommand = new CreateTaskTemplateCommand(
      familyId,
      spotId,
      title,
      new(points),
      scheduleType,
      scheduleTime,
      scheduleDayOfWeek == default ? null : scheduleDayOfWeek,
      scheduleDayOfMonth == default ? null : scheduleDayOfMonth,
      dueDuration,
      session.UserId);

    var result = await mediator.Send(createTemplateCommand, cancellationToken);

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

    var scheduleTime = TimeOnly.MinValue;
    if (scheduleTypeStr != ScheduleTypeManual)
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

    var scheduleType = scheduleTypeStr switch
    {
      ScheduleTypeDaily => ScheduleType.Daily,
      ScheduleTypeWorkdays => ScheduleType.Workdays,
      ScheduleTypeWeekends => ScheduleType.Weekends,
      ScheduleTypeWeekly => ScheduleType.Weekly,
      ScheduleTypeMonthly => ScheduleType.Monthly,
      ScheduleTypeManual => ScheduleType.Manual,
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

    var result = await mediator.Send(updateCommand, cancellationToken);

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

  private static string BuildScheduleDescription(
    string scheduleType,
    TimeOnly time,
    DayOfWeek? dayOfWeek,
    int? dayOfMonth)
  {
    var typeName = ScheduleKeyboardHelper.GetScheduleTypeName(scheduleType);

    return scheduleType switch
    {
      ScheduleTypeManual => typeName,
      ScheduleTypeWeekly when dayOfWeek.HasValue =>
        $"{typeName}, {ScheduleKeyboardHelper.GetWeekdayName(dayOfWeek.Value)} в {time:HH:mm}",
      ScheduleTypeMonthly when dayOfMonth.HasValue => $"{typeName}, {dayOfMonth}-го числа в {time:HH:mm}",
      _ => $"{typeName} в {time:HH:mm}"
    };
  }

  private async Task HandlePointsSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    int points,
    CancellationToken cancellationToken)
  {
    if (!TaskPoints.IsValidValue(points))
      return;

    await botClient.DeleteMessageIfCanAsync(chatId, message, cancellationToken);
    await HandlePointsInputAsync(botClient, chatId, message, session, points.ToString(), cancellationToken);
  }

  private async Task HandleScheduleTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    string scheduleType,
    CancellationToken cancellationToken)
  {
    session.Data.ScheduleType = scheduleType;

    if (scheduleType == ScheduleTypeManual)
    {
      session.Data.InternalState = StateAwaitingDueDuration;
      var keyboard = new ReplyKeyboardMarkup([[new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        BotMessages.Templates.EnterDueDuration,
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      session.Data.InternalState = StateAwaitingScheduleTime;
      var keyboard = new ReplyKeyboardMarkup([[new("❌ Отменить")]])
      {
        ResizeKeyboard = true
      };
      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        "🕐 Введите время выполнения в формате HH:mm (например, 09:00):",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandleWeekdaySelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    string weekday,
    CancellationToken cancellationToken)
  {
    if (!Enum.TryParse<DayOfWeek>(weekday, out var dayOfWeek))
      return;

    session.Data.ScheduleDayOfWeek = dayOfWeek;
    session.Data.InternalState = StateAwaitingDueDuration;
    var keyboard = new ReplyKeyboardMarkup([[new("❌ Отменить")]])
    {
      ResizeKeyboard = true
    };
    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      BotMessages.Templates.EnterDueDuration,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
