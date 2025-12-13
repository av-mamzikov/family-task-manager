using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using FamilyTaskManager.UseCases.Features.SpotManagement.Commands;
using FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;
using FamilyTaskManager.UseCases.Features.SpotManagement.Queries;
using FamilyTaskManager.UseCases.Features.TasksManagement.Queries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class SpotBrowsingHandler(
  ILogger<SpotBrowsingHandler> logger,
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
    if (callbackParts.Length < 2) return;

    if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.View,
          out var viewSpotId))
      await HandleViewSpotAsync(botClient, chatId, message, viewSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.Delete,
               out var deleteSpotId))
      await HandleDeleteSpotAsync(botClient, chatId, message, deleteSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.ConfirmDelete,
               out var confirmDeleteSpotId))
      await HandleConfirmDeleteSpotAsync(botClient, chatId, message, confirmDeleteSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.ResponsibleList,
               out var respSpotId))
      await HandleResponsibleListAsync(botClient, chatId, message, respSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.SpotBrowsing.ResponsibleToggle,
               out var spotId, out EncodedGuid memberId))
      await HandleResponsibleToggleAsync(botClient, chatId, message, spotId, memberId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.SpotBrowsing.List))
      await ShowSpotListAsync(botClient, chatId, message, session, cancellationToken);
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

  public async Task ShowSpotListAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, "❌ Ошибка загрузки спотов", cancellationToken);
      return;
    }

    var spots = spotsResult.Value;

    if (!spots.Any())
    {
      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        "🧩 У вас пока нет спотов.\n\nАдминистратор может создать спота.",
        replyMarkup: new InlineKeyboardMarkup([
          InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.SpotCreation.Start())
        ]),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = BuildSpotListMessage(spots);
    var keyboard = BuildSpotListKeyboard(spots);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleResponsibleListAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var familyMembersResult = await mediator.Send(new GetFamilyMembersQuery(session.CurrentFamilyId.Value),
      cancellationToken);
    if (!familyMembersResult.IsSuccess || familyMembersResult.Value == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, "❌ Ошибка загрузки участников семьи",
        cancellationToken);
      return;
    }

    var responsibleResult = await mediator.Send(new GetSpotResponsibleMembersQuery(spotId), cancellationToken);
    if (!responsibleResult.IsSuccess || responsibleResult.Value == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, "❌ Ошибка загрузки ответственных",
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

      var text = "👥 *Ответственные за спота*\n\n" +
                 "Только взрослые участники семьи могут изменять ответственных.\n\n" +
                 string.Join("\n", lines);

      var keyboardChild = new InlineKeyboardMarkup([
        InlineKeyboardButton.WithCallbackData("⬅️ Назад к споту",
          CallbackData.SpotBrowsing.View(spotId))
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
          CallbackData.SpotBrowsing.ResponsibleToggle(spotId, member.Id))
      ]);
    }

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("⬅️ Назад к споту",
        CallbackData.SpotBrowsing.View(spotId))
    ]);

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "👥 *Ответственные за спота*\n\n" +
      "Нажмите на участника, чтобы назначить или снять ответственность.",
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleResponsibleToggleAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    Guid memberId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Получаем текущих ответственных, чтобы понять, нужно назначить или снять
    var responsibleResult = await mediator.Send(new GetSpotResponsibleMembersQuery(spotId), cancellationToken);
    if (!responsibleResult.IsSuccess || responsibleResult.Value == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, "❌ Ошибка загрузки ответственных",
        cancellationToken);
      return;
    }

    var isResponsible = responsibleResult.Value.Any(m => m.Id == memberId);

    if (isResponsible)
    {
      var command = new RemoveSpotResponsibleCommand(spotId, memberId);
      var removeResult = await mediator.Send(command, cancellationToken);
      if (!removeResult.IsSuccess)
      {
        await EditMessageWithErrorAsync(botClient, chatId, message,
          "❌ Не удалось снять ответственность с участника", cancellationToken);
        return;
      }
    }
    else
    {
      var command = new AssignSpotResponsibleCommand(spotId, memberId);
      var assignResult = await mediator.Send(command, cancellationToken);
      if (!assignResult.IsSuccess)
      {
        await EditMessageWithErrorAsync(botClient, chatId, message,
          "❌ Не удалось назначить участника ответственным", cancellationToken);
        return;
      }
    }

    // После изменения состояния перерисовываем список
    await HandleResponsibleListAsync(botClient, chatId, message, spotId, session, cancellationToken);
  }

  private async Task HandleViewSpotAsync(
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

    var getSpotQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки спота", cancellationToken);
      return;
    }

    var spot = spotsResult.Value.FirstOrDefault(p => p.Id == spotId);
    if (spot == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Спот не найден", cancellationToken);
      return;
    }

    var getTasksQuery = new GetTasksBySpotQuery(spotId, session.CurrentFamilyId.Value, TaskStatus.Active);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
    var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

    var messageText = $"{spotEmoji} *{spot.Name}*\n\n" +
                      $"💖 Настроение: {moodEmoji} - {moodText}\n\n";

    if (tasksResult.IsSuccess && tasksResult.Value.Any())
    {
      messageText += $"📝 *{spot.Name} хочет чтобы вы ему помогли:*\n";
      foreach (var task in tasksResult.Value)
        messageText += $"• {task.Title} {task.Points.ToStars()} до {task.DueAtLocal:dd.MM.yyyy HH:mm}💖\n";
    }
    else
    {
      messageText += $"📝 *Все задачи выполнены, {spot.Name} доволен!*\n";
      messageText += "Нет активных задач. Создайте задачи из шаблонов!";
    }

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("📋 Шаблоны задач", CallbackData.TemplateBrowsing.ListOfSpot(spotId))],
      [
        InlineKeyboardButton.WithCallbackData("👥 Ответственные",
          CallbackData.SpotBrowsing.ResponsibleList(spotId))
      ],
      [InlineKeyboardButton.WithCallbackData("🗑️ Удалить спота", CallbackData.SpotBrowsing.Delete(spotId))],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад к списку", CallbackData.SpotBrowsing.List())]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleDeleteSpotAsync(
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

    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки спота", cancellationToken);
      return;
    }

    var spot = spotsResult.Value.FirstOrDefault(p => p.Id == spotId);
    if (spot == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Спот не найден", cancellationToken);
      return;
    }

    var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("✅ Да, удалить спота", CallbackData.SpotBrowsing.ConfirmDelete(spotId))],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.SpotBrowsing.List())]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      $"⚠️ *Удаление спота*\n\n" +
      $"Вы уверены, что хотите удалить спота {spotEmoji} *{spot.Name}*?\n\n" +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех шаблонов задач спота\n" +
      "• Удалению всех связанных задач\n" +
      "• Настроение и статистика спота перестанут обновляться, но история действий семьи сохранится\n\n" +
      BotMessages.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleConfirmDeleteSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var deleteSpotCommand = new DeleteSpotCommand(spotId, session.UserId);
    var deleteResult = await mediator.Send(deleteSpotCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        message,
        $"❌ Ошибка удаления спота: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "✅ Спот успешно удалён!\n\n" +
      "Все связанные шаблоны задач и задачи также удалены, история действий семьи при этом сохранена.",
      cancellationToken: cancellationToken);
  }

  private static string BuildSpotListMessage(IEnumerable<SpotDto> spots)
  {
    var messageText = "🧩 *Ваши споты:*\n\n";

    foreach (var spot in spots)
    {
      var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
      var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

      messageText += $"{spotEmoji} *{spot.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} - {moodText}\n";
    }

    return messageText;
  }

  private static InlineKeyboardMarkup BuildSpotListKeyboard(IEnumerable<SpotDto> spots)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var spot in spots)
    {
      var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"{spotEmoji} {spot.Name}", CallbackData.SpotBrowsing.View(spot.Id))
      ]);
    }

    buttons.Add([InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.SpotCreation.Start())]);

    return new(buttons);
  }

  private static (string emoji, string text) GetSpotTypeInfo(SpotType spotType) =>
    SpotDisplay.GetInfo(spotType);
}
