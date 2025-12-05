using System.Text;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class FamilyMembersBrousingHandler(
  ILogger<FamilyMembersBrousingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger, mediator), IConversationHandler
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
    var memberAction = callbackParts[1];
    var memberIdStr = callbackParts.Length > 2 ? callbackParts[2] : null;

    var memberId = Guid.Empty;
    if (memberIdStr != null && !TryParseGuid(callbackParts[2], out memberId)) return;

    await (memberAction switch
    {
      CallbackActions.Members =>
        ShowFamilyMembersAsync(botClient, chatId, message, session, cancellationToken),
      CallbackActions.Member =>
        ShowFamilyMemberAsync(botClient, chatId, message, memberId, cancellationToken),

      CallbackActions.MemberRole =>
        ShowRoleSelectionAsync(botClient, chatId, message, memberId, cancellationToken),

      CallbackActions.MemberRolePick when callbackParts.Length >= 4 &&
                                          Enum.TryParse(callbackParts[3], out FamilyRole newRole) =>
        HandleMemberRoleUpdateAsync(botClient, chatId, message, session, memberId, newRole, cancellationToken),

      CallbackActions.MemberDelete =>
        ShowRemoveMemberConfirmationAsync(botClient, chatId, message, memberId, cancellationToken),

      CallbackActions.MemberDeleteOk =>
        HandleMemberRemovalAsync(botClient, chatId, message, session, memberId, cancellationToken),

      _ => botClient.SendTextMessageAsync(chatId, "👥 Действие с участником\n(В разработке)",
        cancellationToken: cancellationToken)
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

  private async Task ShowFamilyMembersAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var result = await Mediator.Send(new GetFamilyMembersQuery(session.CurrentFamilyId!.Value), cancellationToken);
    if (!result.IsSuccess)
    {
      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        $"❌ Ошибка загрузки участников семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    var members = result.Value;
    var messageText = BuildMembersListText(members);
    var keyboard = BuildMembersKeyboard(session.CurrentFamilyId!.Value, members);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task ShowFamilyMemberAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);
    var messageText = $"{roleEmoji} *{member.UserName}*\n\n" +
                      $"Роль: {roleText}\n" +
                      $"Очки: ⭐ {member.Points}";

    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData("♻️ Сменить роль", CallbackData.FamilyMembers.ChangeRole(memberCode)),
        InlineKeyboardButton.WithCallbackData("🗑️ Удалить участника", CallbackData.FamilyMembers.Delete(memberCode))
      ],
      [
        InlineKeyboardButton.WithCallbackData("⬅️ Назад к участникам", CallbackData.Family.List())
      ]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task ShowRoleSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);
    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var availableRoles = Enum.GetValues<FamilyRole>()
      .Where(role => role != member.Role)
      .Select(role => new[]
      {
        InlineKeyboardButton.WithCallbackData(
          BotMessages.Roles.GetRoleText(role),
          CallbackData.FamilyMembers.PickRole(memberCode, (int)role))
      })
      .ToList();

    availableRoles.Add([
      InlineKeyboardButton.WithCallbackData(
        "⬅️ Назад",
        CallbackData.FamilyMembers.Member(memberCode))
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      $"♻️ *Смена роли участника*\n\nТекущая роль: {roleEmoji} {roleText}. Выберите новую роль:",
      ParseMode.Markdown,
      new(availableRoles),
      cancellationToken);
  }

  private async Task ShowRemoveMemberConfirmationAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    var member = await GetMemberAsync(memberId, cancellationToken);
    if (member == null)
    {
      await botClient.SendOrEditMessageAsync(chatId, message, "❌ Участник не найден",
        cancellationToken: cancellationToken);
      return;
    }

    var (roleEmoji, roleText) = GetRoleInfo(member.Role);
    var memberCode = CallbackDataHelper.EncodeGuid(member.Id);

    var keyboard = new InlineKeyboardMarkup([
      [
        InlineKeyboardButton.WithCallbackData(
          "✅ Да, удалить",
          CallbackData.FamilyMembers.ConfirmDelete(memberCode)),
        InlineKeyboardButton.WithCallbackData(
          "❌ Отмена",
          CallbackData.FamilyMembers.Member(memberCode))
      ]
    ]);

    var messageText = $"⚠️ *Удаление участника*\n\n" +
                      $"Вы уверены, что хотите удалить {roleEmoji} *{member.UserName}* ({roleText}) из семьи?";

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleMemberRoleUpdateAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    Guid memberId,
    FamilyRole newRole,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null) return;

    var command = new UpdateFamilyMemberRoleCommand(session.CurrentFamilyId.Value, memberId, session.UserId, newRole);
    var result = await Mediator.Send(command, cancellationToken);

    if (!result.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        message,
        $"❌ Не удалось изменить роль: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await ShowFamilyMemberAsync(botClient, chatId, message, memberId, cancellationToken);
  }

  private async Task HandleMemberRemovalAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    Guid memberId,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null) return;

    var command = new RemoveFamilyMemberCommand(session.CurrentFamilyId.Value, memberId, session.UserId);
    var result = await Mediator.Send(command, cancellationToken);

    if (!result.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        message,
        $"❌ Не удалось удалить участника: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await ShowFamilyMembersAsync(botClient, chatId, message, session, cancellationToken);
  }

  private async Task<FamilyMemberDto?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
  {
    var result = await Mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
    return result.IsSuccess ? result.Value : null;
  }

  private static string BuildMembersListText(List<FamilyMemberDto> members)
  {
    if (!members.Any()) return "👥 *Участники семьи*\n\nВ этой семье пока нет активных участников.";

    var sb = new StringBuilder("👥 *Участники семьи*\n\n");
    foreach (var member in members)
    {
      var (emoji, roleText) = GetRoleInfo(member.Role);
      sb.AppendLine($"{emoji} *{member.UserName}*");
      sb.AppendLine($"   Роль: {roleText}");
      sb.AppendLine($"   Очки: ⭐ {member.Points}\n");
    }

    return sb.ToString();
  }

  private static InlineKeyboardMarkup BuildMembersKeyboard(Guid familyId, List<FamilyMemberDto> members)
  {
    var familyCode = CallbackDataHelper.EncodeGuid(familyId);
    var buttons = members.Select(member =>
    {
      var memberCode = CallbackDataHelper.EncodeGuid(member.Id);
      return new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"{GetRoleInfo(member.Role).emoji} {member.UserName}",
          CallbackData.FamilyMembers.Member(memberCode))
      };
    }).ToList();

    buttons.Add([
      InlineKeyboardButton.WithCallbackData("🔗 Создать приглашение", CallbackData.Family.Invite())
    ]);

    buttons.Add([
      InlineKeyboardButton.WithCallbackData(
        "⬅️ Назад",
        CallbackData.Family.List())
    ]);

    return new(buttons);
  }

  private static (string emoji, string text) GetRoleInfo(FamilyRole role) => role switch
  {
    FamilyRole.Admin => ("👑", "Администратор"),
    FamilyRole.Adult => ("👤", "Взрослый"),
    FamilyRole.Child => ("👶", "Ребёнок"),
    _ => ("❓", "Неизвестно")
  };

  private static bool TryParseGuid(string value, out Guid guid) =>
    Guid.TryParse(value, out guid) || CallbackDataHelper.TryDecodeGuid(value, out guid);
}
