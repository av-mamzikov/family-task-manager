namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskDeletedEvent : DomainEventBase
{
  public Guid TaskId { get; init; }
  public Guid FamilyId { get; init; }
  public required string Title { get; init; }
  public required string Points { get; init; }
  public Guid? DeletedByUserId { get; init; }
  public string? DeletedByUserName { get; init; }
  public long? DeletedByUserTelegramId { get; init; }
  public required string SpotName { get; init; }
  public required string SpotType { get; init; }
}
