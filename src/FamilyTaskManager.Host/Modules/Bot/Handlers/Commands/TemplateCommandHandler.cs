using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
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
        BotConstants.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get pets for the family
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess || !petsResult.Value.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Errors.NoPets,
        cancellationToken: cancellationToken);
      return;
    }

    // Build pet selection keyboard
    var buttons = petsResult.Value.Select(p =>
    {
      var petEmoji = GetPetEmoji(p.Type);
      return new[] { InlineKeyboardButton.WithCallbackData($"{petEmoji} {p.Name}", $"template_viewpet_{p.Id}") };
    }).ToList();

    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", "template_create") });

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "📋 *Управление шаблонами задач*\n\n" +
      "Выберите питомца для просмотра его шаблонов задач или создайте новый:",
      parseMode: ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public virtual async Task HandleViewPetTemplatesAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid petId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        BotConstants.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get templates for pet
    var getTemplatesQuery = new GetTaskTemplatesByPetQuery(petId, session.CurrentFamilyId.Value, true);
    var templatesResult = await mediator.Send(getTemplatesQuery, cancellationToken);

    if (!templatesResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Ошибка загрузки шаблонов",
        cancellationToken: cancellationToken);
      return;
    }

    var templates = templatesResult.Value;

    if (!templates.Any())
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"📋 У питомца *{templates.FirstOrDefault()?.PetName ?? "этого питомца"}* пока нет шаблонов задач.\n\n" +
        "Создайте первый шаблон!",
        ParseMode.Markdown,
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          new[] { InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", $"template_createfor_{petId}") },
          new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "template_back") }
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = $"📋 *Шаблоны задач для {templates.First().PetName}*\n\n";

    foreach (var template in templates)
    {
      messageText += $"📝 *{template.Title}*\n";
      messageText += $"   💯 Очки: {template.Points}\n";
      messageText += $"   🔄 Расписание: `{template.Schedule}`\n";
      messageText += $"   📅 Создан: {template.CreatedAt:dd.MM.yyyy}\n\n";
    }

    // Build buttons for each template
    var buttons = templates.Select(t =>
      new[] { InlineKeyboardButton.WithCallbackData($"✏️ {t.Title}", $"template_view_{t.Id}") }
    ).ToList();

    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", $"template_createfor_{petId}") });
    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "template_back") });

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
        BotConstants.Errors.NoFamily,
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
        cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var messageText = $"📋 *Шаблон задачи*\n\n" +
                      $"📝 Название: *{template.Title}*\n" +
                      $"🐾 Питомец: {template.PetName}\n" +
                      $"💯 Очки: {template.Points}\n" +
                      $"🔄 Расписание: `{template.Schedule}`\n" +
                      $"📅 Создан: {template.CreatedAt:dd.MM.yyyy}\n" +
                      $"✅ Активен: {(template.IsActive ? "Да" : "Нет")}";

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"template_edit_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"template_delete_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"template_viewpet_{template.PetId}") }
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
        BotConstants.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Show confirmation
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"template_confirmdelete_{templateId}") },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", $"template_view_{templateId}") }
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
        BotConstants.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Deactivate template
    var deactivateCommand = new DeactivateTaskTemplateCommand(templateId, session.CurrentFamilyId.Value);
    var result = await mediator.Send(deactivateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка удаления шаблона: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Шаблон успешно удалён!\n\n" +
      "Задачи по этому шаблону больше не будут создаваться автоматически.",
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
        BotConstants.Errors.NoFamily,
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
        cancellationToken: cancellationToken);
      return;
    }

    var template = templateResult.Value;

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✏️ Название", $"template_editfield_{templateId}_title") },
      new[] { InlineKeyboardButton.WithCallbackData("💯 Очки", $"template_editfield_{templateId}_points") },
      new[] { InlineKeyboardButton.WithCallbackData("🔄 Расписание", $"template_editfield_{templateId}_schedule") },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("⏰ Срок выполнения", $"template_editfield_{templateId}_dueduration")
      },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"template_view_{templateId}") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✏️ *Редактирование шаблона*\n\n" +
      $"📝 Название: {template.Title}\n" +
      $"💯 Очки: {template.Points}\n" +
      $"🔄 Расписание: `{template.Schedule}`\n" +
      $"⏰ Срок выполнения: {template.DueDuration.TotalHours} часов\n\n" +
      "Выберите поле для редактирования:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private string GetPetEmoji(PetType type) =>
    type switch
    {
      PetType.Cat => "🐱",
      PetType.Dog => "🐶",
      PetType.Hamster => "🐹",
      _ => "🐾"
    };
}
