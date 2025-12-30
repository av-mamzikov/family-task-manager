namespace FamilyTaskManager.Core.TaskAggregate.Events;

public sealed class TaskCreatedEvent : DomainEventBase
{
  public required Guid TaskId { get; init; }
  public required Guid FamilyId { get; init; }
  public required Guid SpotId { get; init; }
  public required string TaskTitle { get; init; } = string.Empty;
  public required TaskStatus TaskStatus { get; init; }
  public required string SpotName { get; init; } = string.Empty;
  public required string Points { get; init; } = string.Empty;
  public required DateTime DueAt { get; init; }
  public required string Timezone { get; init; } = string.Empty;
  public required Guid? AssignedUserId { get; init; }
  public required string? AssignedUserName { get; init; }
  public required long? AssignedUserTelegramId { get; init; }
}
