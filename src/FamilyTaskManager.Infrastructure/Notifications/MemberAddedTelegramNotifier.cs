using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.FamilyAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends MemberAddedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class MemberAddedTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ILogger<MemberAddedTelegramNotifier> logger)
  : INotificationHandler<MemberAddedEvent>
{
  public async ValueTask Handle(MemberAddedEvent notification, CancellationToken cancellationToken)
  {
    try
    {
      // Get family with members
      var familySpec = new GetFamilyWithMembersSpec(notification.FamilyId);
      var familyWithMembers = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
      if (familyWithMembers == null)
      {
        logger.LogWarning("Family {FamilyId} not found, skipping notification", notification.FamilyId);
        return;
      }

      var activeMembers = familyWithMembers.Members.Where(m => m.IsActive).ToList();
      if (activeMembers.Count == 0)
      {
        logger.LogWarning("No active members in family {FamilyId}, skipping notification", familyWithMembers.Id);
        return;
      }

      // Format message using data from event
      var message = $"👋 <b>Новый член семьи!</b>\n\n" +
                    $"Присоединился: {notification.UserName}";

      // Send to all active family members
      var sendTasks = activeMembers.Select(m =>
        telegramClient.SendTextMessageAsync(
          m.User.TelegramId,
          message,
          parseMode: ParseMode.Html,
          cancellationToken: cancellationToken));

      await Task.WhenAll(sendTasks);

      logger.LogInformation(
        "Sent member added notification for family {FamilyId}",
        notification.FamilyId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send member added notification for family {FamilyId}",
        notification.FamilyId);
      throw;
    }
  }
}
