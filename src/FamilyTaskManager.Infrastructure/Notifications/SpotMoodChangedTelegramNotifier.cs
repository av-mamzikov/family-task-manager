using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.SpotAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends SpotMoodChangedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class SpotMoodChangedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<SpotMoodChangedEvent>
{
  public async ValueTask Handle(SpotMoodChangedEvent notification, CancellationToken cancellationToken)
  {
    // Format message using data from event
    var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(notification.NewMoodScore);

    // First, send a separate emoji-only message to show a big animated emoji in Telegram
    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      moodEmoji,
      [],
      cancellationToken);

    var message = $"{moodEmoji} *Настроение вашего спота изменилось!*\n\n" +
                  $"Спот: {notification.Name}\n" +
                  $"Настроение: {moodEmoji} {moodText}\n" +
                  "Заботьтесь о нём, выполняйте задания и поднимайте настроение всей семьи!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
