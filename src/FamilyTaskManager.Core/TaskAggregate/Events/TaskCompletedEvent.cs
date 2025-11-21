namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCompletedEvent(TaskInstance task) : DomainEventBase
{
  public TaskInstance Task { get; init; } = task;
}
