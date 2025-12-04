using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskCreatedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskCreatedTelegramNotifier(
  ITimeZoneService timeZoneService,
  ITelegramNotificationService telegramNotificationService,
  IMediator mediator)
  : INotificationHandler<TaskCreatedEvent>
{
  public async ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    // Convert DueAt from UTC to family timezone for display
    var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

    var mentionLine = await BuildMentionLineAsync(notification, cancellationToken);

    // Format message using data from event
    var message = $"🎯 *Новая миссия для {notification.SpotName}!*\n\n" +
                  $"Задача: {notification.Title}\n" +
                  $"Награда: {notification.Points}\n" +
                  $"Срок выполнения: {dueAtLocal:dd.MM.yyyy HH:mm}\n" +
                  mentionLine;

    await telegramNotificationService.SendToFamilyMembersAsync(
      notification.FamilyId,
      message,
      [],
      cancellationToken);
  }

  private async Task<string> BuildMentionLineAsync(TaskCreatedEvent notification,
    CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
      new GetNextTaskExecutorQuery(notification.FamilyId, notification.TaskId),
      cancellationToken);

    if (!result.IsSuccess || result.Value is null)
      return string.Empty;

    var executor = result.Value;
    return $"Сегодня очередь героя: [{executor.UserName}](tg://user?id={executor.TelegramId})\n";
  }
}
