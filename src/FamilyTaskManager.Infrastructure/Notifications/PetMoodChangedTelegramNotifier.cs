using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends PetMoodChangedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class PetMoodChangedTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ILogger<PetMoodChangedTelegramNotifier> logger)
  : INotificationHandler<PetMoodChangedEvent>
{
  public async ValueTask Handle(PetMoodChangedEvent notification, CancellationToken cancellationToken)
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
      var moodEmoji = notification.NewMoodScore switch
      {
        >= 80 => "😄",
        >= 60 => "🙂",
        >= 40 => "😐",
        >= 20 => "😟",
        _ => "😢"
      };

      var message = $"{moodEmoji} <b>Настроение питомца изменилось</b>\n\n" +
                    $"Питомец: {notification.Name}\n" +
                    $"Настроение: {notification.NewMoodScore}/100";

      // Send to all active family members
      var sendTasks = activeMembers.Select(member =>
        telegramClient.SendTextMessageAsync(
          member.User.TelegramId,
          message,
          parseMode: ParseMode.Html,
          cancellationToken: cancellationToken));

      await Task.WhenAll(sendTasks);

      logger.LogInformation(
        "Sent pet mood changed notification for pet {PetId} to family {FamilyId}",
        notification.PetId, family.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet mood changed notification for pet {PetId}",
        notification.PetId);
      throw;
    }
  }
}
