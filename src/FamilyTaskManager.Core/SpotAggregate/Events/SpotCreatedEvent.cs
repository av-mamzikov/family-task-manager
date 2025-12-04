namespace FamilyTaskManager.Core.SpotAggregate.Events;

public sealed class SpotCreatedEvent : DomainEventBase
{
  public Guid SpotId { get; init; }
  public Guid FamilyId { get; init; }
  public string Name { get; init; } = string.Empty;
  public string Type { get; init; } = string.Empty;
}
