using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskDeletedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class TaskDeletedTelegramNotifier(
  ITelegramNotificationService telegramNotificationService)
  : INotificationHandler<TaskDeletedEvent>
{
  public async ValueTask Handle(TaskDeletedEvent notification, CancellationToken cancellationToken)
  {
    var message = "🗑️ *Задача удалена*\n\n" +
                  (notification.DeletedByUserId.HasValue
                    ? $"👤 Удалил(а): [{notification.DeletedByUserName}](tg://user?id={notification.DeletedByUserTelegramId})\n"
                    : "") +
                  $"📋 Миссия: {notification.Title}\n" +
                  $"⭐ Сложность: {notification.Points}\n" +
                  $"📍 Объект: {notification.SpotName} ({notification.SpotType})\n\n" +
                  "Задача была автоматически удалена.";

    // Send to all family members except the deleter (if applicable)
    Guid[] excludedUserIds;
    if (notification.DeletedByUserId.HasValue)
      excludedUserIds = [notification.DeletedByUserId.Value];
    else
      excludedUserIds = [];

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      excludedUserIds,
      cancellationToken);
  }
}
