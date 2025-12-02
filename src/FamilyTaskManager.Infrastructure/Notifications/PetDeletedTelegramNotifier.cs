using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends PetDeletedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class PetDeletedTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ILogger<PetDeletedTelegramNotifier> logger)
  : INotificationHandler<PetDeletedEvent>
{
  public async ValueTask Handle(PetDeletedEvent notification, CancellationToken cancellationToken)
  {
    try
    {
      // Get family with members
      var familySpec = new GetFamilyWithMembersSpec(notification.FamilyId);
      var family = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
      if (family == null)
      {
        logger.LogWarning("Family {FamilyId} not found, skipping notification", notification.FamilyId);
        return;
      }

      var activeMembers = family.Members.Where(m => m.IsActive).ToList();
      if (activeMembers.Count == 0)
      {
        logger.LogWarning("No active members in family {FamilyId}, skipping notification", family.Id);
        return;
      }

      // Format message using data from event
      var message = $"😢 <b>Питомец удалён</b>\n\n" +
                    $"Имя: {notification.Name}";

      // Send to all active family members
      var sendTasks = activeMembers.Select(member =>
        telegramClient.SendTextMessageAsync(
          member.User.TelegramId,
          message,
          parseMode: ParseMode.Html,
          cancellationToken: cancellationToken));

      await Task.WhenAll(sendTasks);

      logger.LogInformation(
        "Sent pet deleted notification for pet {PetId} to family {FamilyId}",
        notification.PetId, family.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet deleted notification for pet {PetId}",
        notification.PetId);
      throw;
    }
  }
}
