using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskReminderDueEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskReminderTelegramNotifier(
  ITimeZoneService timeZoneService,
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<TaskReminderDueEvent>
{
  public async ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    if (notification.AssignedUserTelegramId is null)
      return;

    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);
    var mentionLine =
      $"Сегодня твоя очередь, [{notification.AssignedUserName}](tg://user?id={notification.AssignedUserTelegramId})\n";

    // Format message using data from event
    var message = $"⏰ *Личное напоминание герою миссии!*\n" +
                  $"(это сообщение видишь только ты)\n\n" +
                  $"Задача: {notification.Title} для {notification.SpotName}\n" +
                  $"Срок выполнения: {dueAtLocal:HH:mm}\n" +
                  mentionLine +
                  "Пора действовать — выполни миссию и получи баллы!";

    await telegramNotificationService.SendToUserAsync(
      notification.AssignedUserTelegramId.Value,
      message,
      cancellationToken);
  }
}
