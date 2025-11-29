namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskStartedEvent(TaskInstance task) : DomainEventBase
{
  public TaskInstance Task { get; init; } = task;
}
