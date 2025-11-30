using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class FamilyMembersCallbackHandler(
  ILogger<FamilyMembersCallbackHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService,
  FamilyMembersHandler familyMembersHandler)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
{
  private readonly FamilyMembersHandler _familyMembersHandler = familyMembersHandler;

  public async Task HandleMemberActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Family member callback data format: ["family", "action", "memberCode"]
    // All member-specific actions must use 3-part format for Telegram size constraints
    if (parts.Length < 3)
    {
      return;
    }

    var memberAction = parts[1];
    var memberIdStr = parts[2];

    if (!TryParseGuid(memberIdStr, out var memberId))
    {
      return;
    }

    switch (memberAction)
    {
      case "member":
        await _familyMembersHandler.ShowFamilyMemberAsync(botClient, chatId, messageId, memberId,
          cancellationToken);
        break;

      case "memberrole":
        await _familyMembersHandler.ShowRoleSelectionAsync(botClient, chatId, messageId, memberId,
          cancellationToken);
        break;

      case "mrpick":
        if (parts.Length < 4 || !Enum.TryParse(parts[3], out FamilyRole newRole))
        {
          return;
        }

        // Query member to get familyId
        var memberResult = await Mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
        if (!memberResult.IsSuccess)
        {
          return;
        }

        var member = memberResult.Value;

        await HandleMemberRoleUpdateAsync(
          botClient, chatId, messageId, member.FamilyId, memberId, newRole, fromUser, cancellationToken);
        break;

      case "mdel":
        await _familyMembersHandler.ShowRemoveMemberConfirmationAsync(
          botClient, chatId, messageId, memberId, cancellationToken);
        break;

      case "mdelok":
        // Query member to get familyId
        var memberForRemovalResult = await Mediator.Send(new GetFamilyMemberByIdQuery(memberId), cancellationToken);
        if (!memberForRemovalResult.IsSuccess)
        {
          return;
        }

        var memberForRemoval = memberForRemovalResult.Value;

        await HandleMemberRemovalAsync(
          botClient, chatId, messageId, memberForRemoval.FamilyId, memberId, fromUser, cancellationToken);
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
    Guid familyId,
    Guid memberId,
    FamilyRole newRole,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var requesterId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (requesterId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    var command = new UpdateFamilyMemberRoleCommand(familyId, memberId, requesterId.Value, newRole);
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
    Guid familyId,
    Guid memberId,
    User fromUser,
    CancellationToken cancellationToken)
  {
    var requesterId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
    if (requesterId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.UnknownError, cancellationToken);
      return;
    }

    var command = new RemoveFamilyMemberCommand(familyId, memberId, requesterId.Value);
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

    await _familyMembersHandler.ShowFamilyMembersAsync(botClient, chatId, messageId, familyId, cancellationToken);
  }
}
