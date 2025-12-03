namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskStartedEvent : DomainEventBase
{
  public required Guid TaskId { get; init; }
}
