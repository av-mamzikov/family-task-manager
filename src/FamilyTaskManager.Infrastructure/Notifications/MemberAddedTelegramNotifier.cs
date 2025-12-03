using FamilyTaskManager.Core.FamilyAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends MemberAddedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class MemberAddedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<MemberAddedEvent>
{
  public async ValueTask Handle(MemberAddedEvent notification, CancellationToken cancellationToken)
  {
    // Format message using data from event
    var message = $"👋 *В семье пополнение!*\n\n" +
                  $"Теперь с вами новый помощник: {notification.UserName}.\n" +
                  "Организовывать дела и набирать баллы вместе ещё веселее!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [notification.UserId],
      cancellationToken);
  }
}
