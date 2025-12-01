namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Handler for processing outbox events of a specific type.
/// </summary>
public interface IOutboxEventHandler
{
  /// <summary>
  ///   Event type this handler processes (e.g., "TaskCreated", "TaskStarted").
  /// </summary>
  string EventType { get; }

  /// <summary>
  ///   Delivery mode this handler supports.
  /// </summary>
  DeliveryMode DeliveryMode { get; }

  /// <summary>
  ///   Process a single outbox entry.
  /// </summary>
  Task HandleAsync(DomainEventOutbox outboxEntry, CancellationToken cancellationToken = default);

  /// <summary>
  ///   Process a batch of outbox entries (for batched delivery mode).
  ///   Default implementation processes entries one by one.
  /// </summary>
  Task HandleBatchAsync(List<DomainEventOutbox> outboxEntries, CancellationToken cancellationToken = default)
  {
    var tasks = outboxEntries.Select(entry => HandleAsync(entry, cancellationToken));
    return Task.WhenAll(tasks);
  }
}
