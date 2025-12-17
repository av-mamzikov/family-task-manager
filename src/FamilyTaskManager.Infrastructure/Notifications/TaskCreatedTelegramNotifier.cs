using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskCreatedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskCreatedTelegramNotifier(
  ITimeZoneService timeZoneService,
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<TaskCreatedEvent>
{
  public async ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    if (notification.AssignedUserTelegramId is null)
      return;

    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

    var mentionLine =
      $"Сегодня очередь героя: [{notification.AssignedUserName}](tg://user?id={notification.AssignedUserTelegramId})\n";

    // Format message using data from event
    var message = $"🦸 *Личная миссия для героя!*\n" +
                  $"(это сообщение видишь только ты)\n\n" +
                  $"Задача: {notification.Title} для {notification.SpotName}\n" +
                  $"Награда: {notification.Points}\n" +
                  $"Срок выполнения: {dueAtLocal:HH:mm}\n" +
                  mentionLine;

    await telegramNotificationService.SendToUserAsync(
      notification.AssignedUserTelegramId.Value,
      message,
      cancellationToken);
  }
}
