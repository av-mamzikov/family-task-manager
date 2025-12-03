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
    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

    // Format message using data from event
    var message = $"⏰ *Напоминание о миссии!*\n\n" +
                  $"Задача: {notification.Title}\n" +
                  $"Срок выполнения: {dueAtLocal:dd.MM.yyyy HH:mm}\n" +
                  "Не дайте задаче заскучать — выполните её вместе и получите баллы!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
