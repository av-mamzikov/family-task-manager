using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.Utils;
using FamilyTaskManager.Infrastructure.Telegram;
using Mediator;
using Telegram.Bot.Types.ReplyMarkups;

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

    if (notification.AssignedUserId is null)
      return;

    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);
    var mentionLine =
      $"Сегодня твоя очередь, {WikiHelper.GetUserLink(notification.AssignedUserName!, notification.AssignedUserTelegramId!.Value)}\n";

    // Format message using data from event
    var message = $"⏰ *Личное напоминание герою миссии!*\n" +
                  $"(это сообщение видишь только ты)\n\n" +
                  $"Задача: {notification.TaskTitle} для {notification.SpotName}\n" +
                  $"Срок выполнения: {dueAtLocal:HH:mm}\n" +
                  mentionLine +
                  "Пора действовать — выполни миссию и получи баллы!";

    var t = notification.TaskTitle;

    var actions = TaskActionPolicy.GetAvailableActions(
      notification.TaskStatus,
      notification.AssignedUserId,
      notification.AssignedUserId.Value);

    var buttons = actions
      .Where(a => a != TaskAction.Delete)
      .Select(action => TaskActionTelegramButtonFactory.Create(action, notification.TaskId))
      .ToList();

    await telegramNotificationService.SendToUserAsync(
      notification.AssignedUserTelegramId.Value,
      message,
      buttons.Count != 0
        ? new InlineKeyboardMarkup([buttons.ToArray()])
        : null,
      cancellationToken);
  }
}
