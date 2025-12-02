using FamilyTaskManager.Core.PetAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends PetDeletedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class PetDeletedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<PetDeletedEvent>
{
  public async ValueTask Handle(PetDeletedEvent notification, CancellationToken cancellationToken)
  {
    // Format message using data from event
    var message = $"😿 *Питомец удалён*\n\n" +
                  $"{notification.Name} завершил(а) своё приключение в Семейном менеджере дел.\n" +
                  "Вы всегда можете завести нового игрового друга, чтобы продолжить историю!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
