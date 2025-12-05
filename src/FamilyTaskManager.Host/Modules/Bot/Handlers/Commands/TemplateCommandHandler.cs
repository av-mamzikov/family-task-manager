using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class TemplateCommandHandler(IMediator mediator)
{
  public virtual async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Guid userId,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.NoFamily,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Get Spots for the family
    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var SpotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

    if (!SpotsResult.IsSuccess || !SpotsResult.Value.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.NoSpots,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Build Spot selection keyboard
    var buttons = SpotsResult.Value.Select(p =>
    {
      var SpotEmoji = GetSpotEmoji(p.Type);
      return new[]
        { InlineKeyboardButton.WithCallbackData($"{SpotEmoji} {p.Name}", CallbackData.Templates.ViewForSpot(p.Id)) };
    }).ToList();

    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.Templates.CreateRoot) });

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "📋 *Управление шаблонами задач*\n\n" +
      "Выберите спота для просмотра его шаблонов задач или создайте новый:",
      parseMode: ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleViewSpotTemplatesAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid SpotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Get templates for Spot
    var getTemplatesQuery = new GetTaskTemplatesBySpotQuery(SpotId, session.CurrentFamilyId.Value, true);
    var templatesResult = await mediator.Send(getTemplatesQuery, cancellationToken);

    if (!templatesResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Ошибка загрузки шаблонов",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var templates = templatesResult.Value;

    if (!templates.Any())
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"📋 У спота *{templates.FirstOrDefault()?.SpotName ?? "этого спота"}* пока нет шаблонов задач.\n\n" +
        "Создайте первый шаблон!",
        ParseMode.Markdown,
        replyMarkup: new(new[]
        {
          new[]
          {
            InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.Templates.CreateForSpot(SpotId))
          },
          new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Templates.Back) }
        }),
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

    // Build buttons for each template
    var buttons = templates.Select(t =>
      new[] { InlineKeyboardButton.WithCallbackData($"✏️ {t.Title}", CallbackData.Templates.View(t.Id)) }
    ).ToList();

    buttons.Add(new[]
      { InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.Templates.CreateForSpot(SpotId)) });
    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.Templates.Back) });

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleViewTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Get template details
    var getTemplateQuery = new GetTaskTemplateByIdQuery(templateId, session.CurrentFamilyId.Value);
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Шаблон не найден",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var messageText = $"📋 *Шаблон задачи*\n\n" +
                      $"📝 Название: *{template.Title}*\n" +
                      $"🐾 Спот: {template.SpotName}\n" +
                      $"💯 Очки: {template.Points.ToStars()}\n" +
                      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
                      $"🔄 Срок выполнения: `{template.DueDuration}`\n";

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("➕ Создать задачу сейчас", $"tpl_ct_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"tpl_e_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"tpl_d_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"tpl_vp_{template.SpotId}") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleDeleteTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Show confirmation
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"tpl_cd_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", $"tpl_v_{templateId}") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "⚠️ *Удаление шаблона*\n\n" +
      "Вы уверены, что хотите удалить этот шаблон?\n\n" +
      "Это действие нельзя отменить!",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleConfirmDeleteTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Deactivate template
    var deactivateCommand = new DeleteTaskTemplateCommand(templateId, session.CurrentFamilyId.Value);
    var result = await mediator.Send(deactivateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка удаления шаблона: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Шаблон успешно удалён!\n\n" +
      "Задачи по этому шаблону больше не будут создаваться автоматически.",
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleEditTemplateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get template details
    var getTemplateQuery = new GetTaskTemplateByIdQuery(templateId, session.CurrentFamilyId.Value);
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Шаблон не найден",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✏️ Название", $"tpl_ef_{templateId}_t") },
      new[] { InlineKeyboardButton.WithCallbackData("💯 Очки", $"tpl_ef_{templateId}_p") },
      new[] { InlineKeyboardButton.WithCallbackData("🔄 Расписание", $"tpl_ef_{templateId}_s") },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("⏰ Срок выполнения", $"tpl_ef_{templateId}_d")
      },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"tpl_v_{templateId}") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✏️ *Редактирование шаблона*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"🔄 Расписание: {ScheduleFormatter.Format(template.ScheduleType, template.ScheduleTime, template.ScheduleDayOfWeek, template.ScheduleDayOfMonth)}\n" +
      $"⏰ Срок выполнения: {template.DueDuration.TotalHours} часов\n\n" +
      "Выберите поле для редактирования:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleCreateTaskNowAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get template details to show in confirmation
    var getTemplateQuery = new GetTaskTemplateByIdQuery(templateId, session.CurrentFamilyId.Value);
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

    if (!templateResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Шаблон не найден",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    // Create task instance with current time
    var now = DateTime.UtcNow;
    var dueAt = now.Add(template.DueDuration);
    var createCommand = new CreateTaskInstanceFromTemplateCommand(templateId, dueAt);
    var result = await mediator.Send(createCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✅ *Задача создана!*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"🐾 Спот: {template.SpotName}\n" +
      $"💯 Очки: {template.Points.ToStars()}\n" +
      $"⏰ Срок выполнения: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      "Задача добавлена в список активных задач спота.",
      ParseMode.Markdown,
      replyMarkup: new(new[]
      {
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону", $"tpl_v_{templateId}") }
      }),
      cancellationToken: cancellationToken);
  }

  private string GetSpotEmoji(SpotType type) =>
    type switch
    {
      SpotType.Cat => "🐱",
      SpotType.Dog => "🐶",
      SpotType.Hamster => "🐹",
      _ => "🐾"
    };
}
