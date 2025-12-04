using System.Text.Json;
using Ardalis.SharedKernel;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.Infrastructure.Data.Entities;
using Mediator;
using Quartz;

namespace FamilyTaskManager.Infrastructure.Jobs;

/// <summary>
///   Job that processes domain event notifications from outbox.
///   Deserializes task snapshots, reconstructs domain events, and publishes via Mediator.
///   Runs every 1 minute.
/// </summary>
[DisallowConcurrentExecution]
public class OutboxDispatcherJob(
  AppDbContext dbContext,
  IMediator mediator,
  ILogger<OutboxDispatcherJob> logger) : IJob
{
  private const int MaxRetryAttempts = 30;

  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("OutboxDispatcherJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Get all pending notifications
      var pendingNotifications = await dbContext.DomainEventOutbox
        .Where(n => n.Status == NotificationStatus.Pending
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

      var successCount = 0;
      var failureCount = 0;

      // Process each notification individually
      foreach (var entry in pendingNotifications)
        try
        {
          // Reconstruct domain event from snapshot and publish via Mediator
          await ProcessOutboxEntry(entry, context.CancellationToken);

          // Mark as sent
          entry.Status = NotificationStatus.Sent;
          entry.ProcessedAtUtc = DateTime.UtcNow;
          entry.Attempts++;

          successCount++;

          logger.LogDebug(
            "Successfully processed {EventType} notification for entry {EntryId}",
            entry.EventType, entry.Id);
        }
        catch (Exception ex)
        {
          logger.LogError(ex,
            "Failed to process {EventType} notification for entry {EntryId}",
            entry.EventType, entry.Id);

          // Mark as failed and increment attempts
          entry.Attempts++;
          if (entry.Attempts >= MaxRetryAttempts)
          {
            entry.Status = NotificationStatus.Failed;
            entry.ProcessedAtUtc = DateTime.UtcNow;
          }

          failureCount++;
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

  /// <summary>
  ///   Processes a single outbox entry by deserializing the domain event and publishing it.
  ///   Events now contain all data, no need to load entities from DB.
  /// </summary>
  private async Task ProcessOutboxEntry(DomainEventOutbox entry, CancellationToken cancellationToken)
  {
    try
    {
      // Get event type from Core assembly
      var eventType =
        Type.GetType($"FamilyTaskManager.Core.TaskAggregate.Events.{entry.EventType}, FamilyTaskManager.Core") ??
        Type.GetType($"FamilyTaskManager.Core.SpotAggregate.Events.{entry.EventType}, FamilyTaskManager.Core") ??
        Type.GetType($"FamilyTaskManager.Core.FamilyAggregate.Events.{entry.EventType}, FamilyTaskManager.Core") ??
        Type.GetType($"FamilyTaskManager.Core.Points.{entry.EventType}, FamilyTaskManager.Core");

      if (eventType == null)
      {
        logger.LogWarning("Unknown event type {EventType} for entry {EntryId}", entry.EventType, entry.Id);
        return;
      }

      // Deserialize event directly
      var domainEvent = JsonSerializer.Deserialize(entry.Payload, eventType) as IDomainEvent;
      if (domainEvent == null)
      {
        logger.LogWarning("Failed to deserialize event {EventType} for entry {EntryId}", entry.EventType, entry.Id);
        return;
      }

      // Publish via Mediator - Telegram notifiers will handle it
      await mediator.Publish(domainEvent, cancellationToken);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing outbox entry {EntryId}", entry.Id);
      throw;
    }
  }
}
