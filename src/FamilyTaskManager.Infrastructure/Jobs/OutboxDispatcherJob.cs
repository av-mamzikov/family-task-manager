using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Notifications;
using Quartz;

namespace FamilyTaskManager.Infrastructure.Jobs;

/// <summary>
///   Job that processes batched domain event notifications from outbox.
///   Only processes Batched delivery mode events (currently TaskCreated).
///   Runs every 5 minutes.
/// </summary>
[DisallowConcurrentExecution]
public class OutboxDispatcherJob(
  AppDbContext dbContext,
  IEnumerable<IOutboxEventHandler> eventHandlers,
  ILogger<OutboxDispatcherJob> logger) : IJob
{
  private const int MaxRetryAttempts = 30;

  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("OutboxDispatcherJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Get all pending batched notifications
      var pendingNotifications = await dbContext.DomainEventOutbox
        .Where(n => n.Status == NotificationStatus.Pending
                    && n.DeliveryMode == DeliveryMode.Batched
                    && n.Attempts < MaxRetryAttempts)
        .OrderBy(n => n.OccurredAtUtc)
        .ToListAsync(context.CancellationToken);

      if (pendingNotifications.Count == 0)
      {
        logger.LogDebug("No pending notifications to process");
        return;
      }

      logger.LogInformation("Processing {Count} pending notifications",
        pendingNotifications.Count);

      // Group by EventType
      var notificationsByEventType = pendingNotifications
        .GroupBy(n => n.EventType)
        .ToList();

      var successCount = 0;
      var failureCount = 0;

      foreach (var eventTypeGroup in notificationsByEventType)
      {
        var eventType = eventTypeGroup.Key;
        var entries = eventTypeGroup.ToList();

        // Find handler for this event type
        var handler = eventHandlers.FirstOrDefault(h =>
          h.EventType == eventType && h.DeliveryMode == DeliveryMode.Batched);

        if (handler == null)
        {
          logger.LogWarning(
            "No handler found for batched event type {EventType}",
            eventType);
          continue;
        }

        try
        {
          // Process batched events
          await handler.HandleBatchAsync(entries, context.CancellationToken);

          // Mark as sent
          foreach (var entry in entries)
          {
            entry.Status = NotificationStatus.Sent;
            entry.ProcessedAtUtc = DateTime.UtcNow;
            entry.Attempts++;
          }

          successCount += entries.Count;

          logger.LogInformation(
            "Successfully processed {Count} {EventType} notifications",
            entries.Count, eventType);
        }
        catch (Exception ex)
        {
          logger.LogError(ex,
            "Failed to process {Count} {EventType} notifications",
            entries.Count, eventType);

          // Mark as failed and increment attempts
          foreach (var entry in entries)
          {
            entry.Attempts++;
            if (entry.Attempts >= MaxRetryAttempts)
            {
              entry.Status = NotificationStatus.Failed;
              entry.ProcessedAtUtc = DateTime.UtcNow;
            }
          }

          failureCount += entries.Count;
        }
      }

      // Save all changes
      await dbContext.SaveChangesAsync(context.CancellationToken);

      logger.LogInformation(
        "OutboxDispatcherJob completed: {SuccessCount} sent, {FailureCount} failed",
        successCount, failureCount);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "OutboxDispatcherJob failed");
      throw;
    }
  }
}
