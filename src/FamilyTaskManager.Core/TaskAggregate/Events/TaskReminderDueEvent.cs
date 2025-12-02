namespace FamilyTaskManager.Core.TaskAggregate.Events;

/// <summary>
///   Domain event raised when a task reminder should be sent
/// </summary>
public sealed class TaskReminderDueEvent : DomainEventBase
{
  public Guid TaskId { get; init; }
  public Guid FamilyId { get; init; }
  public string Title { get; init; } = string.Empty;
  public DateTime DueAt { get; init; }
  public string Timezone { get; init; } = string.Empty;
}
