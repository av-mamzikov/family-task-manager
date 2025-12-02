using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskCompletedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class TaskCompletedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<TaskCompletedEvent>
{
  public async ValueTask Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
  {
    // Format message using data from event
    var message = $"🎉 *Задача выполнена!*\n\n" +
                  $"👤 Герой: {notification.CompletedByUserName}\n" +
                  $"📋 Миссия: {notification.Title}\n" +
                  $"⭐ Награда: {notification.Points} баллов\n" +
                  "Команда семьи стала ещё сильнее!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [notification.CompletedByUserId],
      cancellationToken);
  }
}
