namespace FamilyTaskManager.Core.TaskAggregate.Events;

/// <summary>
///   Domain event raised when a task reminder should be sent
/// </summary>
public sealed class TaskReminderDueEvent(TaskInstance task) : DomainEventBase
{
  public TaskInstance Task { get; init; } = task;
}
