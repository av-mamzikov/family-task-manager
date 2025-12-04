namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCreatedEvent : DomainEventBase
{
  public Guid TaskId { get; init; }
  public Guid FamilyId { get; init; }
  public Guid SpotId { get; init; }
  public string Title { get; init; } = string.Empty;
  public string SpotName { get; init; } = string.Empty;
  public string Points { get; init; } = string.Empty;
  public DateTime DueAt { get; init; }
  public string Timezone { get; init; } = string.Empty;
}
