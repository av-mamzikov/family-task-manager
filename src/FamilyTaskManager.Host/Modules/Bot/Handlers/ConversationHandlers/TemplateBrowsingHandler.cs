using FamilyTaskManager.Core.SpotAggregate;
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

  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2) return;

    var templateAction = callbackParts[1];

    await (templateAction switch
    {
      CallbackActions.ViewForSpot when callbackParts.Length >= 3 &&
                                       Guid.TryParse(callbackParts[2], out var spotId) =>
        HandleViewSpotTemplatesAsync(botClient, chatId, messageId, spotId, session, cancellationToken),

      CallbackActions.View when callbackParts.Length >= 3 &&
                                Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleViewTemplateAsync(botClient, chatId, messageId, templateId, session, cancellationToken),

      CallbackActions.Delete when callbackParts.Length >= 3 &&
                                  Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleDeleteTemplateAsync(botClient, chatId, messageId, templateId, session, cancellationToken),

      CallbackActions.ConfirmDelete when callbackParts.Length >= 3 &&
                                         Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleConfirmDeleteTemplateAsync(botClient, chatId, messageId, templateId, session, cancellationToken),

      CallbackActions.Edit when callbackParts.Length >= 3 &&
                                Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleEditTemplateAsync(botClient, chatId, messageId, templateId, session, cancellationToken),

      CallbackActions.EditField when callbackParts.Length >= 4 &&
                                     Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleTemplateEditFieldAsync(botClient, chatId, messageId, templateId, callbackParts[3], session,
          cancellationToken),

      CallbackActions.CreateTask when callbackParts.Length >= 3 &&
                                      Guid.TryParse(callbackParts[2], out var templateId) =>
        HandleCreateTaskNowAsync(botClient, chatId, messageId, templateId, session, cancellationToken),

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
    int messageId,
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

    var fieldMap = new Dictionary<string, string>
    {
      { "t", FieldTitle },
      { "p", FieldPoints },
      { "s", FieldSchedule },
      { "d", FieldDueDuration }
    };
    var field = fieldMap.GetValueOrDefault(fieldCode, FieldTitle);

    session.State = ConversationState.TemplateForm;
    session.Data.TemplateId = templateId;

    switch (field)
    {
      case FieldTitle:
        session.Data.InternalState = StateAwaitingTitle;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "✏️ Введите новое название шаблона (от 3 до 100 символов):",
          cancellationToken: cancellationToken);
        break;

      case FieldPoints:
        session.Data.InternalState = StateAwaitingPoints;
        var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard(CallbackData.Templates.Create);
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "⭐ Выберите новую сложность задачи:",
          replyMarkup: pointsKeyboard,
          cancellationToken: cancellationToken);
        break;

      case FieldSchedule:
        session.Data.InternalState = StateAwaitingScheduleType;
        var scheduleTypeKeyboard =
          ScheduleKeyboardHelper.GetScheduleTypeKeyboard(CallbackData.Templates.View(templateId));
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.ChooseScheduleType +
          "\n\n💡 Используйте кнопки для выбора.",
          replyMarkup: scheduleTypeKeyboard,
          cancellationToken: cancellationToken);
        break;

      case FieldDueDuration:
        session.Data.InternalState = StateAwaitingDueDuration;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
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
    int messageId,
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
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Ошибка загрузки шаблонов",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var templates = templatesResult.Value;

    if (!templates.Any())
    {
      await botClient.EditMessageTextAsync(chatId, messageId,
        $"📋 У спота *{templates.FirstOrDefault()?.SpotName ?? "этого спота"}* пока нет шаблонов задач.\n\n" +
        "Создайте первый шаблон!",
        ParseMode.Markdown,
        replyMarkup: new([
          [
            InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.Templates.CreateForSpot(spotId))
          ],
          [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Spot.List())]
        ]),
        cancellationToken: cancellationToken);
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
      new[] { InlineKeyboardButton.WithCallbackData($"✏️ {t.Title}", CallbackData.Templates.View(t.Id)) }
    ).ToList();

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.Templates.CreateForSpot(spotId))
    ]);
    buttons.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Spot.View(spotId))]);

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.EditMessageTextAsync(chatId, messageId, messageText,
      ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
  }

  public async Task HandleViewTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Шаблон не найден",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var messageText = $"📋 *Шаблон задачи*\n\n" +
                      $"📝 Название: *{template.Title}*\n" +
                      $"🐾 Спот: {template.SpotName}\n" +
                      $"💯 Очки: {template.Points.ToStars()}\n" +
                      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
                      $"🔄 Срок выполнения: `{template.DueDuration}`\n";

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("➕ Создать задачу сейчас", $"tpl_ct_{templateId}")],
      [InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"tpl_e_{templateId}")],
      [InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"tpl_d_{templateId}")],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"tpl_vp_{template.SpotId}")]
    ]);

    await botClient.EditMessageTextAsync(chatId, messageId, messageText,
      ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
  }

  private async Task HandleDeleteTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
      [InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"tpl_cd_{templateId}")],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", $"tpl_v_{templateId}")]
    ]);

    await botClient.EditMessageTextAsync(chatId, messageId,
      "⚠️ *Удаление шаблона*\n\n" +
      "Вы уверены, что хотите удалить этот шаблон?\n\n" +
      "Это действие нельзя отменить!",
      ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
  }

  private async Task HandleConfirmDeleteTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
      await botClient.EditMessageTextAsync(chatId, messageId,
        $"❌ Ошибка удаления шаблона: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(chatId, messageId,
      "✅ Шаблон успешно удалён!\n\n" +
      "Задачи по этому шаблону больше не будут создаваться автоматически.",
      ParseMode.Markdown, cancellationToken: cancellationToken);
  }

  public async Task HandleEditTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Шаблон не найден",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("✏️ Название", $"tpl_ef_{templateId}_t")],
      [InlineKeyboardButton.WithCallbackData("💯 Очки", $"tpl_ef_{templateId}_p")],
      [InlineKeyboardButton.WithCallbackData("🔄 Расписание", $"tpl_ef_{templateId}_s")],
      [InlineKeyboardButton.WithCallbackData("⏰ Срок выполнения", $"tpl_ef_{templateId}_d")],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"tpl_v_{templateId}")]
    ]);

    await botClient.EditMessageTextAsync(chatId, messageId,
      $"✏️ *Редактирование шаблона*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
      $"⏰ Срок выполнения: {template.DueDuration.TotalHours} часов\n\n" +
      "Выберите поле для редактирования:",
      ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: cancellationToken);
  }

  private async Task HandleCreateTaskNowAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
      await botClient.EditMessageTextAsync(chatId, messageId, "❌ Шаблон не найден",
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
      await botClient.EditMessageTextAsync(chatId, messageId,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown, cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(chatId, messageId,
      $"✅ *Задача создана!*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"🐾 Спот: {template.SpotName}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"⏰ Срок выполнения: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      "Задача добавлена в список активных задач спота.",
      ParseMode.Markdown,
      replyMarkup: new([
        [InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону", $"tpl_v_{templateId}")]
      ]),
      cancellationToken: cancellationToken);
  }

  private static string GetSpotEmoji(SpotType type) =>
    type switch
    {
      SpotType.Cat => "🐱",
      SpotType.Dog => "🐶",
      SpotType.Hamster => "🐹",
      _ => "🐾"
    };
}
