using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.Utils;
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
                  $"👤 Герой: {WikiHelper.GetUserLink(notification.CompletedByUserName, notification.CompletedByUserTelegramId)}\n" +
                  $"🧩 Спот: {SpotDisplay.GetEmoji(notification.SpotType)} {notification.SpotName}\n" +
                  $"📋 Миссия: {notification.Title}\n" +
                  $"⭐ Награда: {notification.Points}\n" +
                  "Команда семьи стала ещё сильнее!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }
}
