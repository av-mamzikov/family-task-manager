namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCompletedEvent : DomainEventBase
{
  public Guid TaskId { get; init; }
  public Guid FamilyId { get; init; }
  public required string Title { get; init; }
  public required string Points { get; init; }
  public required Guid CompletedByUserId { get; init; }
  public required string CompletedByUserName { get; init; }
  public required long CompletedByUserTelegramId { get; init; }
}
