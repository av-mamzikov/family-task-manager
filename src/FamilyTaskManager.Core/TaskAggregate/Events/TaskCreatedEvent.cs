namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCreatedEvent : DomainEventBase
{
  public required Guid TaskId { get; init; }
  public required Guid FamilyId { get; init; }
  public required Guid SpotId { get; init; }
  public Guid? AssignedToMemberId { get; init; }
  public required string Title { get; init; } = string.Empty;
  public required string SpotName { get; init; } = string.Empty;
  public required string Points { get; init; } = string.Empty;
  public required DateTime DueAt { get; init; }
  public required string Timezone { get; init; } = string.Empty;
}
