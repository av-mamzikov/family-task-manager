namespace FamilyTaskManager.Infrastructure.Data.Entities;

/// <summary>
///   Universal outbox for all domain event notifications.
///   Ensures reliable delivery even after service restarts.
///   Supports both immediate and batched delivery modes.
/// </summary>
public class DomainEventOutbox
{
  public Guid Id { get; set; }
  public string EventType { get; set; } = string.Empty;
  public string Payload { get; set; } = string.Empty; // JSON
  public DateTime OccurredAtUtc { get; set; }
  public DateTime? ProcessedAtUtc { get; set; }
  public int Attempts { get; set; }
  public NotificationStatus Status { get; set; }
}

public enum NotificationStatus
{
  Pending = 0,
  Sent = 1,
  Failed = 2
}
