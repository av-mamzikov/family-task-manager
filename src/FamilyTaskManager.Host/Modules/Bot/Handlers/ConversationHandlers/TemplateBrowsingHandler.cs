using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;
using FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateBrowsingHandler(
  ILogger<TemplateBrowsingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger), IConversationHandler
{
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
    if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.ListOfSpot, out EncodedGuid spotId))
      await HandleViewSpotTemplatesAsync(botClient, chatId, message, spotId.Value, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.View, out EncodedGuid viewTemplateId))
      await HandleViewTemplateAsync(botClient, chatId, message, viewTemplateId.Value, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.Delete, out EncodedGuid deleteTemplateId))
      await HandleDeleteTemplateAsync(botClient, chatId, message, deleteTemplateId.Value, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.ConfirmDelete,
               out EncodedGuid confirmDeleteTemplateId))
      await HandleConfirmDeleteTemplateAsync(botClient, chatId, message, confirmDeleteTemplateId.Value, session,
        cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.CreateTask, out EncodedGuid createTaskTemplateId))
      await HandleCreateTaskNowAsync(botClient, chatId, message, createTaskTemplateId.Value, session,
        cancellationToken);
    // Новые обработчики для ответственности
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.ResponsibleList,
               out EncodedGuid responsibleListTemplateId))
      await HandleResponsibleListAsync(botClient, chatId, message, responsibleListTemplateId.Value, session,
        cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.TemplateBrowsing.ResponsibleToggle,
               out var templateId, out EncodedGuid memberId))
      await HandleResponsibleToggleAsync(botClient, chatId, message, templateId.Value, memberId.Value, session,
        cancellationToken);
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
    var templatesResult = await mediator.Send(getTemplatesQuery, cancellationToken);

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
        new InlineKeyboardMarkup([
          [
            InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.TemplateForm.Create(spotId))
          ],
          [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.SpotBrowsing.List())]
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
      InlineKeyboardButton.WithCallbackData("➕ Создать шаблон", CallbackData.TemplateForm.Create(spotId))
    ]);
    buttons.Add([InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.SpotBrowsing.View(spotId))]);

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
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

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
      [
        InlineKeyboardButton.WithCallbackData("👥 Ответственные",
          CallbackData.TemplateBrowsing.ResponsibleList(templateId))
      ],
      [InlineKeyboardButton.WithCallbackData("🗑️ Удалить", CallbackData.TemplateBrowsing.Delete(templateId))],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", CallbackData.SpotBrowsing.View(template.SpotId))]
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
    var result = await mediator.Send(deactivateCommand, cancellationToken);

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
    var templateResult = await mediator.Send(getTemplateQuery, cancellationToken);

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
    var result = await mediator.Send(createCommand, cancellationToken);

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
      new InlineKeyboardMarkup([
        [InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону", CallbackData.TemplateBrowsing.View(templateId))]
      ]),
      cancellationToken);
  }

  private async Task HandleResponsibleListAsync(
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

    var familyMembersResult = await mediator.Send(new GetFamilyMembersQuery(session.CurrentFamilyId.Value),
      cancellationToken);
    if (!familyMembersResult.IsSuccess || familyMembersResult.Value == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки участников семьи",
        cancellationToken);
      return;
    }

    var responsibleResult =
      await mediator.Send(new GetTaskTemplateResponsibleMembersQuery(templateId), cancellationToken);
    if (!responsibleResult.IsSuccess || responsibleResult.Value == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки ответственных",
        cancellationToken);
      return;
    }

    var members = familyMembersResult.Value;
    var responsibleIds = responsibleResult.Value.Select(m => m.Id).ToHashSet();

    // Определяем текущего участника семьи по UserId
    var currentMember = members.FirstOrDefault(m => m.UserId == session.UserId);
    var isChild = currentMember?.Role == FamilyRole.Child;

    if (isChild)
    {
      // Для детей показываем только текстовый список без кнопок-тогглов
      var lines = new List<string>();
      foreach (var member in members)
      {
        var isResponsible = responsibleIds.Contains(member.Id);
        var prefix = isResponsible ? "✅ " : string.Empty;
        lines.Add($"{prefix}{RoleDisplay.GetRoleEmoji(member.Role)} {member.UserName}");
      }

      var text = "👥 *Ответственные за шаблон задачи*\n\n" +
                 "Только взрослые участники семьи могут изменять ответственных.\n\n" +
                 string.Join("\n", lines);

      var keyboardChild = new InlineKeyboardMarkup([
        InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону",
          CallbackData.TemplateBrowsing.View(templateId))
      ]);

      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        text,
        ParseMode.Markdown,
        keyboardChild,
        cancellationToken);
      return;
    }

    // Для взрослых/админов показываем список участников как кнопки с возможностью toggle
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var member in members)
    {
      var isResponsible = responsibleIds.Contains(member.Id);
      var prefix = isResponsible ? "✅ " : string.Empty;
      var text = $"{prefix}{RoleDisplay.GetRoleEmoji(member.Role)} {member.UserName}";
      buttons.Add([
        InlineKeyboardButton.WithCallbackData(text,
          CallbackData.TemplateBrowsing.ResponsibleToggle(templateId, member.Id))
      ]);
    }

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("⬅️ Назад к шаблону",
        CallbackData.TemplateBrowsing.View(templateId))
    ]);

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "👥 *Ответственные за шаблон задачи*\n\n" +
      "Нажмите на участника, чтобы назначить или снять ответственность.",
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleResponsibleToggleAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid templateId,
    Guid memberId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Получаем текущих ответственных, чтобы понять, нужно назначить или снять
    var responsibleResult =
      await mediator.Send(new GetTaskTemplateResponsibleMembersQuery(templateId), cancellationToken);
    if (!responsibleResult.IsSuccess || responsibleResult.Value == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки ответственных",
        cancellationToken);
      return;
    }

    var isResponsible = responsibleResult.Value.Any(m => m.Id == memberId);

    if (isResponsible)
    {
      var command = new RemoveTaskTemplateResponsibleCommand(templateId, memberId);
      var removeResult = await mediator.Send(command, cancellationToken);
      if (!removeResult.IsSuccess)
      {
        await SendErrorAsync(botClient, chatId,
          "❌ Не удалось снять ответственность с участника", cancellationToken);
        return;
      }
    }
    else
    {
      var command = new AssignTaskTemplateResponsibleCommand(templateId, memberId);
      var assignResult = await mediator.Send(command, cancellationToken);
      if (!assignResult.IsSuccess)
      {
        await SendErrorAsync(botClient, chatId,
          "❌ Не удалось назначить участника ответственным", cancellationToken);
        return;
      }
    }

    // После изменения состояния перерисовываем список
    await HandleResponsibleListAsync(botClient, chatId, message, templateId, session, cancellationToken);
  }
}
