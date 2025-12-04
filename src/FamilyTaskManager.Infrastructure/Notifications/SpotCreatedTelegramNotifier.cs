using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.SpotAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends SpotCreatedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class SpotCreatedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<SpotCreatedEvent>
{
  public async ValueTask Handle(SpotCreatedEvent notification, CancellationToken cancellationToken)
  {
    // Determine emoji for Spot type using shared Core helper
    var emoji = SpotDisplay.GetEmojiFromCode(notification.Type);

    // Format message using data from event
    var message = $"{emoji} *Новый спот в семье!*\n\n" +
                  $"{notification.Name} поселился в вашем Семейном менеджере дел.\n" +
                  "Дарите заботу, выполняйте задания и зарабатывайте баллы вместе!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
