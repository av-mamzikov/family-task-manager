using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class FamilyMembersCallbackHandler(
  ILogger<FamilyMembersCallbackHandler> logger,
  IMediator mediator,
  FamilyMembersHandler familyMembersHandler)
  : BaseCallbackHandler(logger, mediator), ICallbackHandler
{
  private readonly FamilyMembersHandler _familyMembersHandler = familyMembersHandler;

  public async Task Handle(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken) =>
    await HandleMemberActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken);

  public async Task HandleMemberActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Family member callback data format: ["family", "action", "memberCode"]
    // All member-specific actions must use 3-part format for Telegram size constraints
    if (parts.Length < 3) return;

    var memberAction = parts[1];
    var memberIdStr = parts[2];

    if (!TryParseGuid(memberIdStr, out var memberId)) return;

    switch (memberAction)
    {
      case var _ when memberAction == CallbackActions.Member:
        await _familyMembersHandler.ShowFamilyMemberAsync(botClient, chatId, messageId, memberId,
          cancellationToken);
        break;

      case var _ when memberAction == CallbackActions.MemberRole:
        await _familyMembersHandler.ShowRoleSelectionAsync(botClient, chatId, messageId, memberId,
          cancellationToken);
        break;

      case var _ when memberAction == CallbackActions.MemberRolePick:
        if (parts.Length < 4 || !Enum.TryParse(parts[3], out FamilyRole newRole)) return;

        // Query member to get familyId
        var memberResult = await Mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
        if (!memberResult.IsSuccess) return;

        await HandleMemberRoleUpdateAsync(
          botClient, chatId, messageId, session, memberId, newRole, cancellationToken);
        break;

      case var _ when memberAction == CallbackActions.MemberDelete:
        await _familyMembersHandler.ShowRemoveMemberConfirmationAsync(
          botClient, chatId, messageId, memberId, cancellationToken);
        break;

      case var _ when memberAction == CallbackActions.MemberDeleteOk:
        // Query member to get familyId
        var memberForRemovalResult = await Mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
        if (!memberForRemovalResult.IsSuccess) return;

        await HandleMemberRemovalAsync(
          botClient, chatId, messageId, session, memberId, cancellationToken);
        break;

      default:
        await botClient.SendTextMessageAsync(
          chatId,
          "👥 Действие с участником\n(В разработке)",
          cancellationToken: cancellationToken);
        break;
    }
  }

  private static bool TryParseGuid(string value, out Guid guid) =>
    Guid.TryParse(value, out guid) || CallbackDataHelper.TryDecodeGuid(value, out guid);

  private async Task HandleMemberRoleUpdateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    Guid memberId,
    FamilyRole newRole,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
      return;
    var command = new UpdateFamilyMemberRoleCommand(session.CurrentFamilyId.Value, memberId, session.UserId, newRole);
    var result = await Mediator.Send(command, cancellationToken);

    if (!result.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Не удалось изменить роль: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await _familyMembersHandler.ShowFamilyMemberAsync(botClient, chatId, messageId, memberId, cancellationToken);
  }

  private async Task HandleMemberRemovalAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
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
        messageId,
        $"❌ Не удалось удалить участника: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await _familyMembersHandler.ShowFamilyMembersAsync(botClient, chatId, messageId, session.CurrentFamilyId.Value,
      cancellationToken);
  }
}
