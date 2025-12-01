using System.Text.Json;
using FamilyTaskManager.Infrastructure.Notifications.Payloads;

namespace FamilyTaskManager.Infrastructure.Notifications.Handlers;

/// <summary>
///   Handles batched task creation notifications from outbox.
/// </summary>
public class TaskCreatedOutboxHandler(
  ITelegramNotificationService notificationService,
  ILogger<TaskCreatedOutboxHandler> logger) : IOutboxEventHandler
{
  public string EventType => "TaskCreatedEvent";
  public DeliveryMode DeliveryMode => DeliveryMode.Batched;

  public Task HandleAsync(DomainEventOutbox outboxEntry, CancellationToken cancellationToken = default) => throw
    // For batched mode, this shouldn't be called directly
    new NotSupportedException("TaskCreated events should be processed in batches via HandleBatchAsync");

  public async Task HandleBatchAsync(List<DomainEventOutbox> outboxEntries,
    CancellationToken cancellationToken = default)
  {
    if (outboxEntries.Count == 0) return;

    // Group by FamilyId
    var groupedByFamily = outboxEntries
      .Select(e => new
      {
        Entry = e,
        Payload = JsonSerializer.Deserialize<TaskCreatedNotificationPayload>(e.Payload)
      })
      .Where(x => x.Payload != null)
      .GroupBy(x => x.Payload!.FamilyId)
      .ToList();

    foreach (var familyGroup in groupedByFamily)
    {
      var familyId = familyGroup.Key;
      var tasks = familyGroup.Select(x => new TaskCreatedNotificationDto
      {
        TaskId = x.Payload!.TaskId,
        TaskTitle = x.Payload.TaskTitle,
        Points = x.Payload.Points,
        PetName = x.Payload.PetName,
        DueAtFamilyTz = x.Payload.DueAtFamilyTz
      }).ToList();

      try
      {
        await notificationService.SendTaskCreatedBatchAsync(familyId, tasks, cancellationToken);

        logger.LogInformation(
          "Successfully sent batch of {Count} task creation notifications to family {FamilyId}",
          tasks.Count, familyId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Failed to send batch of {Count} task creation notifications to family {FamilyId}",
          tasks.Count, familyId);
        throw;
      }
    }
  }
}

/// <summary>
///   DTO for batched task creation notifications.
/// </summary>
public class TaskCreatedNotificationDto
{
  public Guid TaskId { get; set; }
  public string TaskTitle { get; set; } = string.Empty;
  public string Points { get; set; } = string.Empty;
  public string PetName { get; set; } = string.Empty;
  public DateTime DueAtFamilyTz { get; set; }
}
