using System.Text.Json;
using FamilyTaskManager.Infrastructure.Notifications.Payloads;

namespace FamilyTaskManager.Infrastructure.Notifications.Handlers;

/// <summary>
///   Processes batched task reminder notifications from outbox.
/// </summary>
public class TaskReminderOutboxHandler(
  ITelegramNotificationService notificationService,
  ILogger<TaskReminderOutboxHandler> logger) : IOutboxEventHandler
{
  public string EventType => "TaskReminderDueEvent";
  public DeliveryMode DeliveryMode => DeliveryMode.Batched;

  public Task HandleAsync(DomainEventOutbox outboxEntry, CancellationToken cancellationToken = default) => throw
    // Task reminders should be processed in batches
    new NotSupportedException("TaskReminder events should be processed in batches via HandleBatchAsync");

  public async Task HandleBatchAsync(List<DomainEventOutbox> outboxEntries,
    CancellationToken cancellationToken = default)
  {
    if (outboxEntries.Count == 0)
    {
      logger.LogWarning("HandleBatchAsync called with empty list");
      return;
    }

    logger.LogInformation("Processing batch of {Count} task reminder notifications", outboxEntries.Count);

    // Group by FamilyId
    var remindersByFamily = outboxEntries
      .Select(entry => JsonSerializer.Deserialize<TaskReminderNotificationPayload>(entry.Payload))
      .Where(payload => payload != null)
      .GroupBy(payload => payload!.FamilyId)
      .ToList();

    foreach (var familyGroup in remindersByFamily)
    {
      var familyId = familyGroup.Key;
      var reminders = familyGroup
        .Select(p => new TaskReminderBatchDto
        {
          TaskId = p!.TaskId,
          Title = p.TaskTitle,
          DueAt = p.DueAtFamilyTz
        })
        .ToList();

      try
      {
        await notificationService.SendTaskReminderBatchAsync(familyId, reminders, cancellationToken);

        logger.LogInformation(
          "Sent batch of {Count} task reminders to family {FamilyId}",
          reminders.Count, familyId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Failed to send batch of task reminders to family {FamilyId}",
          familyId);
        throw;
      }
    }
  }
}

/// <summary>
///   DTO for batched task reminder notification.
/// </summary>
public class TaskReminderBatchDto
{
  public Guid TaskId { get; set; }
  public string Title { get; set; } = string.Empty;
  public DateTime DueAt { get; set; }
}
