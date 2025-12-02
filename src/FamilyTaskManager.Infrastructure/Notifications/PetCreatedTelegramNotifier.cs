using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.PetAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends PetCreatedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class PetCreatedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<PetCreatedEvent>
{
  public async ValueTask Handle(PetCreatedEvent notification, CancellationToken cancellationToken)
  {
    // Determine emoji for pet type using shared Core helper
    var emoji = PetDisplay.GetEmojiFromCode(notification.Type);

    // Format message using data from event
    var message = $"{emoji} *Новый питомец в семье!*\n\n" +
                  $"{notification.Name} поселился в вашем Семейном менеджере дел.\n" +
                  "Дарите заботу, выполняйте задания и зарабатывайте баллы вместе!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
