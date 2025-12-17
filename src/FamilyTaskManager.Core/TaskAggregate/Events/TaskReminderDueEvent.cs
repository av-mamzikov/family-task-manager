namespace FamilyTaskManager.Core.TaskAggregate.Events;

/// <summary>
///   Domain event raised when a task reminder should be sent
/// </summary>
public sealed class TaskReminderDueEvent : DomainEventBase
{
  public required Guid TaskId { get; init; }
  public required Guid FamilyId { get; init; }
  public required Guid? TemplateId { get; init; }
  public required string SpotName { get; init; } = string.Empty;
  public required string Title { get; init; } = string.Empty;
  public required DateTime DueAt { get; init; }
  public required string Timezone { get; init; } = string.Empty;
  public required string? AssignedUserName { get; init; }
  public required long? AssignedUserTelegramId { get; init; }
}
