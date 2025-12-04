using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class FamilyCallbackHandler(
  ILogger<FamilyCallbackHandler> logger,
  IMediator mediator,
  BotInfoService botInfoService,
  FamilyMembersHandler familyMembersHandler,
  FamilyMembersCallbackHandler familyMembersCallbackHandler)
  : BaseCallbackHandler(logger, mediator)
{
  private readonly FamilyMembersCallbackHandler _familyMembersCallbackHandler = familyMembersCallbackHandler;
  private readonly FamilyMembersHandler _familyMembersHandler = familyMembersHandler;

  public async Task StartCreateFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    session.SetState(ConversationState.AwaitingFamilyName, new());

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✏️ Введите название семьи (минимум 3 символа):",
      cancellationToken: cancellationToken);
  }

  public async Task HandleFamilySelectionAsync(
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
      BotConstants.Success.FamilySelected + BotConstants.Success.NextStepsMessage,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  public async Task HandleFamilyActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3) return;

    var familyAction = parts[1];

    // For most actions, parts[2] is familyId; for member-specific actions we may also have userId
    var familyIdStr = parts.Length > 2 ? parts[2] : string.Empty;

    if (!TryParseGuid(familyIdStr, out var familyId)) return;

    switch (familyAction)
    {
      case "invite":
        await HandleCreateInviteAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
        break;

      case "members":
        await _familyMembersHandler.ShowFamilyMembersAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "back":
        await HandleFamilyBackAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
        break;

      case "member":
      case "memberrole":
      case "mrpick":
      case "mdel":
      case "mdelok":
        await _familyMembersCallbackHandler.HandleMemberActionAsync(botClient, chatId, messageId, parts, session,
          fromUser,
          cancellationToken);
        break;

      case "settings":
        await HandleFamilySettingsAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "delete":
        await HandleDeleteFamilyAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      default:
        await botClient.SendTextMessageAsync(
          chatId,
          "🏠 Действие с семьей\n(В разработке)",
          cancellationToken: cancellationToken);
        break;
    }
  }

  public async Task HandleInviteActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 4) return;

    var inviteAction = parts[1];
    var familyIdStr = parts[2];
    var roleStr = parts[3];

    if (!Guid.TryParse(familyIdStr, out var familyId)) return;

    if (!Enum.TryParse<FamilyRole>(roleStr, out var role)) return;

    // Create invite code
    var createInviteCommand = new CreateInviteCodeCommand(familyId, role, session.UserId);
    var result = await Mediator.Send(createInviteCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAsync(
        botClient,
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var inviteCode = result.Value;

    if (!botInfoService.IsInitialized || string.IsNullOrEmpty(botInfoService.Username))
      throw new InvalidOperationException("Bot username is not available. Please ensure the bot is fully started.");

    var botUsername = botInfoService.Username;
    var inviteLink = $"https://t.me/{botUsername}?start=invite_{inviteCode}";

    var roleText = BotConstants.Roles.GetRoleText(role);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✅ *Приглашение создано!*\n\n" +
      $"🔗 Ссылка для приглашения:\n[Открыть бота и принять приглашение]({inviteLink})\n\n" +
      $"👤 Роль: {roleText}\n" +
      $"🔑 Код: `{inviteCode}`\n" +
      $"⏰ Действительно 7 дней\n\n" +
      BotConstants.Messages.SendInviteLink,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  public async Task HandleConfirmDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Delete the family
    var deleteFamilyCommand = new DeleteFamilyCommand(familyId, session.UserId);
    var deleteResult = await Mediator.Send(deleteFamilyCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка удаления семьи: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    // Clear current family if it was the deleted one
    if (session.CurrentFamilyId == familyId)
    {
      session.CurrentFamilyId = null;

      // Try to select another family if user has any remaining
      var getFamiliesQuery = new GetUserFamiliesQuery(session.UserId);
      var familiesResult = await Mediator.Send(getFamiliesQuery, cancellationToken);

      if (familiesResult.IsSuccess && familiesResult.Value.Any())
        session.CurrentFamilyId = familiesResult.Value.First().Id;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Семья успешно удалена!\n\n" +
      BotConstants.Messages.FamilyDeleted,
      cancellationToken: cancellationToken);
  }

  private static bool TryParseGuid(string value, out Guid guid) =>
    Guid.TryParse(value, out guid) || CallbackDataHelper.TryDecodeGuid(value, out guid);

  private async Task HandleCreateInviteAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Show role selection
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("👑 Администратор", $"invite_role_{familyId}_Admin") },
      new[] { InlineKeyboardButton.WithCallbackData("👤 Взрослый", $"invite_role_{familyId}_Adult") },
      new[] { InlineKeyboardButton.WithCallbackData("👶 Ребёнок", $"invite_role_{familyId}_Child") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🔗 *Создание приглашения*\n\nВыберите роль для нового участника:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyBackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var familiesResult = await Mediator.Send(new GetUserFamiliesQuery(session.UserId), cancellationToken);
    if (!familiesResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка загрузки семей: {familiesResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var families = familiesResult.Value;
    if (!families.Any())
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        BotConstants.Messages.NoFamilies,
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          new[] { InlineKeyboardButton.WithCallbackData("➕ Создать семью", "create_family") }
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var activeFamilyId = families.Any(f => f.Id == familyId)
      ? familyId
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
      messageText += $"   Роль: {roleEmoji} {BotConstants.Roles.GetRoleText(family.UserRole)}\n";
      messageText += $"   Очки: ⭐ {family.UserPoints}\n\n";
    }

    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var family in families)
      if (family.Id != session.CurrentFamilyId)
        buttons.Add(new[]
        {
          InlineKeyboardButton.WithCallbackData(
            $"Переключиться на \"{family.Name}\"",
            $"select_family_{family.Id}")
        });

    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать новую семью", "create_family") });

    var currentFamily = families.FirstOrDefault(f => f.Id == session.CurrentFamilyId);
    if (currentFamily?.UserRole == FamilyRole.Admin)
    {
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("👥 Управление участниками", $"family_members_{session.CurrentFamilyId}"),
        InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение", $"family_invite_{session.CurrentFamilyId}")
      });
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⚙️ Настройки семьи", $"family_settings_{session.CurrentFamilyId}"),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить семью", $"family_delete_{session.CurrentFamilyId}")
      });
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyMembersAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    var query = new GetFamilyMembersQuery(familyId);
    var result = await Mediator.Send(query, cancellationToken);

    if (!result.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка загрузки участников семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var members = result.Value;

    var messageText = "\ud83d\udc65 *Участники семьи*\n\n";

    if (!members.Any())
      messageText += "В этой семье пока нет активных участников.";
    else
      foreach (var member in members)
      {
        var roleText = BotConstants.Roles.GetRoleText(member.Role);
        var roleEmoji = member.Role switch
        {
          FamilyRole.Admin => "👑",
          FamilyRole.Adult => "👤",
          FamilyRole.Child => "👶",
          _ => "❓"
        };

        messageText += $"{roleEmoji} *{member.UserName}*\n" +
                       $"   Роль: {roleText}\n" +
                       $"   Очки: ⭐ {member.Points}\n\n";
      }

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение", $"family_invite_{familyId}")
      }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySettingsAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      "⚙️ Настройки семьи\n(В разработке)",
      cancellationToken: cancellationToken);

  private async Task HandleDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    // Show confirmation dialog
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить семью", $"confirm_delete_{familyId}") },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel_delete") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "⚠️ *Удаление семьи*\n\n" +
      BotConstants.Messages.ConfirmFamilyDeletion +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех участников семьи\n" +
      "• Удалению всех спотов\n" +
      "• Удалению всех задач и их истории\n" +
      "• Удалению всей статистики\n\n" +
      BotConstants.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
