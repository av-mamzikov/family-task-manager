namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCreatedEvent(TaskInstance task) : DomainEventBase
{
  public TaskInstance Task { get; init; } = task;
}
