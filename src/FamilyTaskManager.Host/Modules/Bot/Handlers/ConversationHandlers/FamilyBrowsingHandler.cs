using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.Infrastructure.Telegram;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using CallbackData = FamilyTaskManager.Infrastructure.Telegram.CallbackData;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class FamilyBrowsingHandler(
  ILogger<FamilyBrowsingHandler> logger,
  IMediator mediator,
  BotInfoService botInfoService)
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

    if (callbackParts.IsCallbackOf(CallbackData.Family.Create))
      await StartCreateFamilyAsync(botClient, chatId, message, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.Family.Select,
               out var familyId))
      await HandleFamilySelectionAsync(botClient, chatId, message, familyId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.Family.List))
      await ShowFamilyListAsync(botClient, chatId, message, session.CurrentFamilyId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.Family.Invite))
      await HandleCreateInviteAsync(botClient, chatId, message, session.CurrentFamilyId!.Value, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.Family.InviteRole,
               out var inviteFamilyId, out string roleString) &&
             Enum.TryParse<FamilyRole>(roleString, out var role))
      await HandleInviteRoleAsync(botClient, chatId, message, inviteFamilyId, role, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.Family.Settings))
      await HandleFamilySettingsAsync(botClient, chatId, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.Family.Delete))
      await HandleDeleteFamilyAsync(botClient, chatId, message, session.CurrentFamilyId!.Value, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.Family.ConfirmDelete,
               out var deleteFamilyId))
      await HandleConfirmDeleteFamilyAsync(botClient, chatId, message, deleteFamilyId, session, cancellationToken);
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

  public async Task ShowFamilyListAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid? currentFamilyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var familiesResult = await mediator.Send(new GetUserFamiliesQuery(session.UserId), cancellationToken);
    if (!familiesResult.IsSuccess)
    {
      if (message != null)
        await EditMessageWithErrorAsync(botClient, chatId, message,
          $"❌ Ошибка загрузки семей: {familiesResult.Errors.FirstOrDefault()}", cancellationToken);
      else
        await SendErrorAsync(botClient, chatId,
          $"❌ Ошибка загрузки семей: {familiesResult.Errors.FirstOrDefault()}", cancellationToken);
      return;
    }

    var families = familiesResult.Value;
    if (!families.Any())
    {
      var keyboard = new InlineKeyboardMarkup([
        [InlineKeyboardButton.WithCallbackData("➕ Создать семью", CallbackData.Family.Create())]
      ]);

      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        BotMessages.Messages.NoFamilies,
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
      return;
    }

    var activeFamilyId = currentFamilyId.HasValue && families.Any(f => f.Id == currentFamilyId)
      ? currentFamilyId.Value
      : families.First().Id;
    session.CurrentFamilyId = activeFamilyId;

    var messageText = "🏠 *Ваши семьи:*\n\n";
    foreach (var family in families)
    {
      var isActive = family.Id == session.CurrentFamilyId;
      var marker = isActive ? "✅" : "⚪";
      var (roleEmoji, roleText) = RoleDisplay.GetRoleInfo(family.UserRole);

      messageText += $"{marker} *{family.Name}*\n";
      messageText += $"   Роль: {roleEmoji} {roleText}\n";
      messageText += $"   Очки: ⭐ {family.UserPoints}\n\n";
    }

    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var family in families)
      if (family.Id != session.CurrentFamilyId)
        buttons.Add([
          InlineKeyboardButton.WithCallbackData($"Переключиться на \"{family.Name}\"",
            CallbackData.Family.Select(family.Id))
        ]);

    buttons.Add([InlineKeyboardButton.WithCallbackData("➕ Создать новую семью", CallbackData.Family.Create())]);

    var currentFamily = families.FirstOrDefault(f => f.Id == session.CurrentFamilyId);
    if (currentFamily?.UserRole == FamilyRole.Admin)
    {
      buttons.Add([
        InlineKeyboardButton.WithCallbackData("👥 Управление участниками", CallbackData.FamilyMembers.List()),
        InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение",
          CallbackData.Family.Invite())
      ]);
      buttons.Add([
        InlineKeyboardButton.WithCallbackData("⚙️ Настройки семьи",
          CallbackData.Family.Settings()),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить семью",
          CallbackData.Family.Delete())
      ]);
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      new InlineKeyboardMarkup(buttons),
      cancellationToken);
  }

  private async Task StartCreateFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.FamilyCreation;
    session.Data = new() { InternalState = "awaiting_name" };

    await botClient.SendOrEditMessageAsync(
      chatId,
      messageId,
      "✏️ Введите название семьи (минимум 3 символа):",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    EncodedGuid familyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.CurrentFamilyId = familyId;

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      BotMessages.Success.FamilySelected + BotMessages.Success.NextStepsMessage,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleCreateInviteAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var keyboard = new InlineKeyboardMarkup(
      new[] { FamilyRole.Admin, FamilyRole.Adult, FamilyRole.Child }
        .Select(role => new[]
        {
          InlineKeyboardButton.WithCallbackData(RoleDisplay.GetRoleCaption(role),
            CallbackData.Family.InviteRole(familyId, role.ToString()))
        }));

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "🔗 *Создание приглашения*\n\nВыберите роль для нового участника:",
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleInviteRoleAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid familyId,
    FamilyRole role,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var createInviteCommand = new CreateInviteCodeCommand(familyId, role, session.UserId);
    var result = await mediator.Send(createInviteCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, $"❌ Ошибка: {result.Errors.FirstOrDefault()}", cancellationToken);
      return;
    }

    var inviteCode = result.Value;

    if (!botInfoService.IsInitialized || string.IsNullOrEmpty(botInfoService.Username))
      throw new InvalidOperationException("Bot username is not available. Please ensure the bot is fully started.");

    var botUsername = botInfoService.Username;
    var inviteLink = $"https://t.me/{botUsername}?start=invite_{inviteCode}";
    var roleText = RoleDisplay.GetRoleCaption(role);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      $"✅ *Приглашение создано!*\n\n" +
      $"🔗 Ссылка для приглашения:\n[Открыть бота и принять приглашение]({inviteLink})\n\n" +
      $"👤 Роль: {roleText}\n" +
      $"🔑 Код: `{inviteCode}`\n" +
      $"⏰ Действительно 7 дней\n\n" +
      BotMessages.Messages.SendInviteLink,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySettingsAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(chatId, "⚙️ Настройки семьи\n(В разработке)",
      cancellationToken: cancellationToken);

  private async Task HandleDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("✅ Да, удалить семью", CallbackData.Family.ConfirmDelete(familyId))
      ],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.Family.List())]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "⚠️ *Удаление семьи*\n\n" +
      BotMessages.Messages.ConfirmFamilyDeletion +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех участников семьи\n" +
      "• Удалению всех спотов\n" +
      "• Удалению всех задач и их истории\n" +
      "• Удалению всей статистики\n\n" +
      BotMessages.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleConfirmDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid familyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var deleteFamilyCommand = new DeleteFamilyCommand(familyId, session.UserId);
    var deleteResult = await mediator.Send(deleteFamilyCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message,
        $"❌ Ошибка удаления семьи: {deleteResult.Errors.FirstOrDefault()}", cancellationToken);
      return;
    }

    if (session.CurrentFamilyId == familyId)
    {
      session.CurrentFamilyId = null;

      var getFamiliesQuery = new GetUserFamiliesQuery(session.UserId);
      var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

      if (familiesResult.IsSuccess && familiesResult.Value.Any())
        session.CurrentFamilyId = familiesResult.Value.First().Id;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "✅ Семья успешно удалена!\n\n" + BotMessages.Messages.FamilyDeleted,
      cancellationToken: cancellationToken);
  }
}
