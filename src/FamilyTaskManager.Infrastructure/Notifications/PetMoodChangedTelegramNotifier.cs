using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.PetAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends PetMoodChangedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class PetMoodChangedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<PetMoodChangedEvent>
{
  public async ValueTask Handle(PetMoodChangedEvent notification, CancellationToken cancellationToken)
  {
    // Format message using data from event
    var (moodEmoji, moodText) = PetDisplay.GetMoodInfo(notification.NewMoodScore);

    var message = $"{moodEmoji} *Настроение вашего питомца изменилось!*\n\n" +
                  $"Питомец: {notification.Name}\n" +
                  $"Настроение: {moodEmoji} {moodText}\n" +
                  "Заботьтесь о нём, выполняйте задания и поднимайте настроение всей семьи!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
