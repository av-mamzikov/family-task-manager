using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateBrowsingHandler(
  ILogger<TemplateBrowsingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger, mediator), IConversationHandler
{
  private const string FieldTitle = "title";
  private const string FieldPoints = "points";
  private const string FieldSchedule = "schedule";
  private const string FieldDueDuration = "dueduration";

  private const string StateAwaitingTitle = "awaiting_title";
  private const string StateAwaitingPoints = "awaiting_points";
  private const string StateAwaitingScheduleType = "awaiting_schedule_type";
  private const string StateAwaitingDueDuration = "awaiting_due_duration";

  public Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task HandleCallbackAsync(ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2) return;

    var templateAction = callbackParts[1];

    await (templateAction switch
    {
      CallbackActions.ListForSpot when callbackParts.Length >= 3 &&
                                       Guid.TryParse(callbackParts[2], out var spotId) =>
        HandleViewSpotTemplatesAsync(botClient, chatId, message, spotId, session, cancellationToken),

      CallbackActions.View when callbackParts.Length >= 3 && Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleViewTemplateAsync(botClient, chatId, message, templateId, session, cancellationToken),

      CallbackActions.Delete when callbackParts.Length >= 3 &&
                                  Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleDeleteTemplateAsync(botClient, chatId, message, templateId, session, cancellationToken),

      CallbackActions.ConfirmDelete when callbackParts.Length >= 3 &&
                                         Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleConfirmDeleteTemplateAsync(botClient, chatId, message, templateId, session, cancellationToken),

      CallbackActions.Edit when callbackParts.Length >= 3 &&
                                Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleEditTemplateAsync(botClient, chatId, message, templateId, session, cancellationToken),

      CallbackActions.Edit when callbackParts.Length >= 4 &&
                                Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleTemplateEditFieldAsync(botClient, chatId, message, templateId, callbackParts[3], session,
          cancellationToken),

      CallbackActions.CreateTask when callbackParts.Length >= 3 &&
                                      Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleCreateTaskNowAsync(botClient, chatId, message, templateId, session, cancellationToken),

      _ => SendErrorAsync(botClient, chatId, "❌ Неизвестное действие", cancellationToken)
    });
  }


  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    await sendMainMenuAction();
    session.ClearState();
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
          TaskPointsHelper.GetPointsSelectionKeyboard(CallbackData.TemplateBrowsing.View(templateId));
        await botClient.SendOrEditMessageAsync(
          chatId,
          message,
          "⭐ Выберите новую сложность задачи:",
          replyMarkup: pointsKeyboard,
          cancellationToken: cancellationToken);
        break;

      case FieldSchedule:
        session.Data.InternalState = StateAwaitingScheduleType;
        var scheduleTypeKeyboard =
          ScheduleKeyboardHelper.GetScheduleTypeKeyboard(CallbackData.TemplateBrowsing.View(templateId));
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

  public async Task HandleViewSpotTemplatesAsync(
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

    var getTemplatesQuery = new GetTaskTemplatesBySpotQuery(spotId, session.CurrentFamilyId.Value, true);
    var templatesResult = await Mediator.Send(getTemplatesQuery, cancellationToken);

    if (!templatesResult.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Ошибка загрузки шаблонов",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var templates = templatesResult.Value;

    if (!templates.Any())
    {
      await botClient.SendOrEditMessageAsync(chatId, message,
        $"📋 У спота *{templates.FirstOrDefault()?.SpotName ?? "этого спота"}* пока нет шаблонов задач.\n\n" +
        "Создайте первый шаблон!",
        ParseMode.Markdown,
        new([
          [
            InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.TemplateBrowsing.Create(spotId))
          ],
          [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Spot.List())]
        ]),
        cancellationToken);
      return;
    }

    var messageText = $"📋 *Шаблоны задач для {templates.First().SpotName}*\n\n";

    foreach (var template in templates)
    {
      messageText += $"📝 *{template.Title}*\n";
      messageText += $"   💯 Очки: {template.Points.ToStars()}\n";
      messageText +=
        $"   🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n";
      messageText += $"   📅 Создан: {template.CreatedAt:dd.MM.yyyy}\n\n";
    }

    var buttons = templates.Select(t =>
      new[] { InlineKeyboardButton.WithCallbackData($"✏️ {t.Title}", CallbackData.TemplateBrowsing.View(t.Id)) }
    ).ToList();

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.TemplateBrowsing.View(spotId))
    ]);
    buttons.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Spot.View(spotId))]);

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendOrEditMessageAsync(chatId, message, messageText,
      ParseMode.Markdown, keyboard, cancellationToken);
  }

  public async Task HandleViewTemplateAsync(
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
    var templateResult = await Mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Шаблон не найден",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var messageText = $"📋 *Шаблон задачи*\n\n" +
                      $"📝 Название: *{template.Title}*\n" +
                      $"🧩 Спот: {template.SpotName}\n" +
                      $"💯 Очки: {template.Points.ToStars()}\n" +
                      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
                      $"🔄 Срок выполнения: `{template.DueDuration}`\n";

    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("➕ Создать задачу сейчас",
          CallbackData.TemplateBrowsing.CreateTask(templateId))
      ],
      [InlineKeyboardButton.WithCallbackData("✏️ Редактировать", CallbackData.TemplateForm.Edit(templateId))],
      [InlineKeyboardButton.WithCallbackData("🗑️ Удалить", CallbackData.TemplateBrowsing.Delete(templateId))],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Spot.View(template.SpotId))]
    ]);

    await botClient.SendOrEditMessageAsync(chatId, message, messageText,
      ParseMode.Markdown, keyboard, cancellationToken);
  }

  private async Task HandleDeleteTemplateAsync(
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

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("✅ Да, удалить", CallbackData.TemplateBrowsing.ConfirmDelete(templateId))],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.TemplateBrowsing.View(templateId))]
    ]);

    await botClient.SendOrEditMessageAsync(chatId, message,
      "⚠️ *Удаление шаблона*\n\n" +
      "Вы уверены, что хотите удалить этот шаблон?\n\n" +
      "Это действие нельзя отменить!",
      ParseMode.Markdown, keyboard, cancellationToken);
  }

  private async Task HandleConfirmDeleteTemplateAsync(
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

    var deactivateCommand = new DeleteTaskTemplateCommand(templateId, session.CurrentFamilyId.Value);
    var result = await Mediator.Send(deactivateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message,
        $"❌ Ошибка удаления шаблона: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(chatId, message,
      "✅ Шаблон успешно удалён!\n\n" +
      "Задачи по этому шаблону больше не будут создаваться автоматически.",
      ParseMode.Markdown, cancellationToken: cancellationToken);
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
    var templateResult = await Mediator.Send(getTemplateQuery, cancellationToken);

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
          CallbackData.TemplateBrowsing.EditField(templateId, FieldTitle))
      ],
      [
        InlineKeyboardButton.WithCallbackData("💯 Очки",
          CallbackData.TemplateBrowsing.EditField(templateId, FieldPoints))
      ],
      [
        InlineKeyboardButton.WithCallbackData("🔄 Расписание",
          CallbackData.TemplateBrowsing.EditField(templateId, FieldSchedule))
      ],
      [
        InlineKeyboardButton.WithCallbackData("⏰ Срок выполнения",
          CallbackData.TemplateBrowsing.EditField(templateId, FieldDueDuration))
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

  private async Task HandleCreateTaskNowAsync(
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
    var templateResult = await Mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Шаблон не найден",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var now = DateTime.UtcNow;
    var dueAt = now.Add(template.DueDuration);
    var createCommand = new CreateTaskInstanceFromTemplateCommand(templateId, dueAt);
    var result = await Mediator.Send(createCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(chatId, message,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(chatId, message,
      $"✅ *Задача создана!*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"🧩 Спот: {template.SpotName}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"⏰ Срок выполнения: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      "Задача добавлена в список активных задач спота.",
      ParseMode.Markdown,
      new([
        [InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону", CallbackData.TemplateBrowsing.View(templateId))]
      ]),
      cancellationToken);
  }
}
