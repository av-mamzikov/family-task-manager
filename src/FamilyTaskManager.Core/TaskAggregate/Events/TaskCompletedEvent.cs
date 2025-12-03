namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCompletedEvent : DomainEventBase
{
  public Guid TaskId { get; init; }
  public Guid FamilyId { get; init; }
  public string Title { get; init; } = string.Empty;
  public string Points { get; init; } = string.Empty;
  public Guid CompletedByUserId { get; init; }
  public string CompletedByUserName { get; init; } = string.Empty;
}
