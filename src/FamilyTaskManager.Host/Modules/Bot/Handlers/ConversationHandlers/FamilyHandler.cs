using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class FamilyHandler(
  ILogger<FamilyHandler> logger,
  IMediator mediator,
  BotInfoService botInfoService)
  : BaseConversationHandler(logger, mediator), IConversationHandler
{
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

    var familyAction = callbackParts[1];

    if (familyAction == CallbackActions.Create)
    {
      await StartCreateFamilyAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    if (familyAction == CallbackActions.Select && callbackParts.Length >= 3)
    {
      await HandleFamilySelectionAsync(botClient, chatId, messageId, callbackParts[2], session, cancellationToken);
      return;
    }

    if (callbackParts.Length < 2) return;

    var familyId = session.CurrentFamilyId!.Value;

    await (familyAction switch
    {
      CallbackActions.Invite =>
        HandleInviteActionAsync(botClient, chatId, messageId, callbackParts, familyId, session, cancellationToken),

      CallbackActions.Settings =>
        HandleFamilySettingsAsync(botClient, chatId, cancellationToken),

      CallbackActions.Delete =>
        HandleDeleteFamilyAsync(botClient, chatId, messageId, familyId, cancellationToken),

      CallbackActions.ConfirmDelete =>
        HandleConfirmDeleteFamilyAsync(botClient, chatId, messageId, familyId, session, cancellationToken),

      CallbackActions.List =>
        ShowFamilyListAsync(botClient, chatId, messageId, familyId, session, cancellationToken),

      _ => Task.CompletedTask
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

  public async Task ShowFamilyListAsync(
    ITelegramBotClient botClient,
    long chatId,
    int? messageId,
    Guid? currentFamilyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var familiesResult = await Mediator.Send(new GetUserFamiliesQuery(session.UserId), cancellationToken);
    if (!familiesResult.IsSuccess)
    {
      if (messageId.HasValue)
        await EditMessageWithErrorAsync(botClient, chatId, messageId.Value,
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
        [InlineKeyboardButton.WithCallbackData("➕ Создать семью", CallbackData.Family.Create)]
      ]);

      if (messageId.HasValue)
        await botClient.EditMessageTextAsync(chatId, messageId.Value, BotMessages.Messages.NoFamilies,
          replyMarkup: keyboard, cancellationToken: cancellationToken);
      else
        await botClient.SendTextMessageAsync(chatId, BotMessages.Messages.NoFamilies,
          replyMarkup: keyboard, cancellationToken: cancellationToken);
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
      var roleEmoji = family.UserRole switch
      {
        FamilyRole.Admin => "👑",
        FamilyRole.Adult => "👤",
        FamilyRole.Child => "👶",
        _ => "❓"
      };

      messageText += $"{marker} *{family.Name}*\n";
      messageText += $"   Роль: {roleEmoji} {BotMessages.Roles.GetRoleText(family.UserRole)}\n";
      messageText += $"   Очки: ⭐ {family.UserPoints}\n\n";
    }

    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var family in families)
      if (family.Id != session.CurrentFamilyId)
        buttons.Add([
          InlineKeyboardButton.WithCallbackData($"Переключиться на \"{family.Name}\"",
            CallbackData.Family.Select())
        ]);

    buttons.Add([InlineKeyboardButton.WithCallbackData("➕ Создать новую семью", CallbackData.Family.Create)]);

    var currentFamily = families.FirstOrDefault(f => f.Id == session.CurrentFamilyId);
    if (currentFamily?.UserRole == FamilyRole.Admin)
    {
      buttons.Add([
        InlineKeyboardButton.WithCallbackData("👥 Управление участниками", CallbackData.FamilyMembers.Members()),
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

    if (messageId.HasValue)
      await botClient.EditMessageTextAsync(chatId, messageId.Value, messageText,
        ParseMode.Markdown, replyMarkup: new(buttons), cancellationToken: cancellationToken);
    else
      await botClient.SendTextMessageAsync(chatId, messageText,
        parseMode: ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(buttons),
        cancellationToken: cancellationToken);
  }

  private async Task StartCreateFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.FamilyCreation;
    session.Data = new() { InternalState = "awaiting_name" };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✏️ Введите название семьи (минимум 3 символа):",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string familyIdStr,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (!TryParseGuid(familyIdStr, out var familyId)) return;

    session.CurrentFamilyId = familyId;

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotMessages.Success.FamilySelected + BotMessages.Success.NextStepsMessage,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleInviteActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    Guid familyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length >= 5 && parts[2] == "role" && Enum.TryParse<FamilyRole>(parts[4], out var role))
      await HandleInviteRoleAsync(botClient, chatId, messageId, familyId, role, session, cancellationToken);
    else
      await HandleCreateInviteAsync(botClient, chatId, messageId, familyId, cancellationToken);
  }

  private async Task HandleCreateInviteAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("👑 Администратор",
          CallbackData.Family.InviteRole(familyId, nameof(FamilyRole.Admin)))
      ],
      [
        InlineKeyboardButton.WithCallbackData("👤 Взрослый",
          CallbackData.Family.InviteRole(familyId, nameof(FamilyRole.Adult)))
      ],
      [
        InlineKeyboardButton.WithCallbackData("👶 Ребёнок",
          CallbackData.Family.InviteRole(familyId, nameof(FamilyRole.Child)))
      ]
    ]);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🔗 *Создание приглашения*\n\nВыберите роль для нового участника:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleInviteRoleAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    FamilyRole role,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var createInviteCommand = new CreateInviteCodeCommand(familyId, role, session.UserId);
    var result = await Mediator.Send(createInviteCommand, cancellationToken);

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
    var roleText = BotMessages.Roles.GetRoleText(role);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
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
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("✅ Да, удалить семью", CallbackData.Family.ConfirmDelete(familyId))
      ],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.Family.List())]
    ]);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "⚠️ *Удаление семьи*\n\n" +
      BotMessages.Messages.ConfirmFamilyDeletion +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех участников семьи\n" +
      "• Удалению всех спотов\n" +
      "• Удалению всех задач и их истории\n" +
      "• Удалению всей статистики\n\n" +
      BotMessages.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleConfirmDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var deleteFamilyCommand = new DeleteFamilyCommand(familyId, session.UserId);
    var deleteResult = await Mediator.Send(deleteFamilyCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId,
        $"❌ Ошибка удаления семьи: {deleteResult.Errors.FirstOrDefault()}", cancellationToken);
      return;
    }

    if (session.CurrentFamilyId == familyId)
    {
      session.CurrentFamilyId = null;

      var getFamiliesQuery = new GetUserFamiliesQuery(session.UserId);
      var familiesResult = await Mediator.Send(getFamiliesQuery, cancellationToken);

      if (familiesResult.IsSuccess && familiesResult.Value.Any())
        session.CurrentFamilyId = familiesResult.Value.First().Id;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Семья успешно удалена!\n\n" + BotMessages.Messages.FamilyDeleted,
      cancellationToken: cancellationToken);
  }


  private static bool TryParseGuid(string value, out Guid guid) =>
    Guid.TryParse(value, out guid) || CallbackDataHelper.TryDecodeGuid(value, out guid);
}
