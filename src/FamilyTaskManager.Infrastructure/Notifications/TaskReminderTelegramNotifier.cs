using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskReminderDueEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskReminderTelegramNotifier(
  ITimeZoneService timeZoneService,
  ITelegramNotificationService telegramNotificationService,
  IMediator mediator,
  IAppRepository<User> userAppRepository)
  : INotificationHandler<TaskReminderDueEvent>
{
  public async ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);
    var mentionLine = await BuildMentionLineAsync(notification, cancellationToken);

    // Format message using data from event
    var message = $"⏰ *Напоминание о миссии!*\n\n" +
                  $"Задача: {notification.Title}\n" +
                  $"Срок выполнения: {dueAtLocal:dd.MM.yyyy HH:mm}\n" +
                  mentionLine +
                  "Не дайте задаче заскучать — выполните её вместе и получите баллы!";

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }

  private async Task<string> BuildMentionLineAsync(TaskReminderDueEvent notification,
    CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new GetNextTaskExecutorQuery(notification.FamilyId, notification.TaskId),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
      return string.Empty;

    var user = await userAppRepository.GetByIdAsync(result.Value, cancellationToken);
    if (user is null)
      return string.Empty;

    return $"Сегодня очередь героя: [{user.Name}](tg://user?id={user.TelegramId})\n";
  }
}
