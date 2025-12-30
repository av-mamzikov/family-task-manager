using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.Utils;
using FamilyTaskManager.Infrastructure.Telegram;
using Mediator;
using Telegram.Bot.Types.ReplyMarkups;

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
    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

    var actions = TaskActionPolicy.GetAvailableActions(
      notification.TaskStatus,
      notification.AssignedUserId,
      notification.AssignedUserId);

    var buttons = actions
      .Where(a => a != TaskAction.Delete)
      .Select(action => TaskActionTelegramButtonFactory.Create(action, notification.TaskId))
      .ToList();

    var keyboard = buttons.Count != 0
      ? new InlineKeyboardMarkup([buttons.ToArray()])
      : null;

    if (notification.AssignedUserTelegramId is null)
    {
      var message = $"🗺️ *Общая миссия открыта!*\n" +
                    $"Никто ещё не назначен — кто-то из вас может взять квест.\n\n" +
                    $"Задача: {notification.TaskTitle} для {notification.SpotName}\n" +
                    $"Награда: {notification.Points}\n" +
                    $"Срок выполнения: {dueAtLocal:HH:mm}\n\n" +
                    $"Первый герой, который выполнит — забирает славу и очки!";

      await telegramNotificationService.SendToFamilyMembersAsync(
        notification.FamilyId,
        message,
        [],
        keyboard,
        cancellationToken);
    }
    else
    {
      var mentionLine =
        $"Сегодня твоя очередь, {WikiHelper.GetUserLink(notification.AssignedUserName!, notification.AssignedUserTelegramId!.Value)}\n";

      // Format message using data from event
      var assignedMessage = $"🦸 *Личная миссия для героя!*\n" +
                            $"(это сообщение видишь только ты)\n\n" +
                            $"Задача: {notification.TaskTitle} для {notification.SpotName}\n" +
                            $"Награда: {notification.Points}\n" +
                            $"Срок выполнения: {dueAtLocal:HH:mm}\n" +
                            mentionLine;

      await telegramNotificationService.SendToUserAsync(
        notification.AssignedUserTelegramId.Value,
        assignedMessage,
        keyboard,
        cancellationToken);
    }
  }
}
