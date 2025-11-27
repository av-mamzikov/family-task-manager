using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Configuration;
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
  IUserRegistrationService userRegistrationService,
  BotConfiguration botConfiguration)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
{
  public async Task StartCreateFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    session.SetState(ConversationState.AwaitingFamilyName,
      new Dictionary<string, object> { ["userId"] = userId.Value });

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
    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

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
    if (parts.Length < 3)
    {
      return;
    }

    var familyAction = parts[1];
    var familyIdStr = parts[2];

    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

    switch (familyAction)
    {
      case "invite":
        await HandleCreateInviteAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
        break;

      case "members":
        await HandleFamilyMembersAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "settings":
        await HandleFamilySettingsAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "delete":
        await HandleDeleteFamilyAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
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
    if (parts.Length < 4)
    {
      return;
    }

    var inviteAction = parts[1];
    var familyIdStr = parts[2];
    var roleStr = parts[3];

    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

    if (!Enum.TryParse<FamilyRole>(roleStr, out var role))
    {
      return;
    }

    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    // Create invite code
    var createInviteCommand = new CreateInviteCodeCommand(familyId, role, userId.Value);
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
    var botUsername = botConfiguration.BotUsername;
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
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    // Delete the family
    var deleteFamilyCommand = new DeleteFamilyCommand(familyId, userId.Value);
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
      var getFamiliesQuery = new GetUserFamiliesQuery(userId.Value);
      var familiesResult = await Mediator.Send(getFamiliesQuery, cancellationToken);

      if (familiesResult.IsSuccess && familiesResult.Value.Any())
      {
        session.CurrentFamilyId = familiesResult.Value.First().Id;
      }
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Семья успешно удалена!\n\n" +
      BotConstants.Messages.FamilyDeleted,
      cancellationToken: cancellationToken);
  }

  private async Task HandleCreateInviteAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

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

  private async Task HandleFamilyMembersAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      "👥 Управление участниками\n(В разработке)",
      cancellationToken: cancellationToken);

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
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (userId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

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
      "• Удалению всех питомцев\n" +
      "• Удалению всех задач и их истории\n" +
      "• Удалению всей статистики\n\n" +
      BotConstants.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
