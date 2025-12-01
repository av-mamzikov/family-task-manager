namespace FamilyTaskManager.Infrastructure.Notifications.Payloads;

/// <summary>
///   Payload for task created notification events.
///   Serialized to JSON and stored in DomainEventOutbox.
/// </summary>
public class TaskCreatedNotificationPayload
{
  public Guid FamilyId { get; set; }
  public Guid TaskId { get; set; }
  public string TaskTitle { get; set; } = string.Empty;
  public string Points { get; set; } = string.Empty;
  public string PetName { get; set; } = string.Empty;
  public DateTime DueAtFamilyTz { get; set; }
}
