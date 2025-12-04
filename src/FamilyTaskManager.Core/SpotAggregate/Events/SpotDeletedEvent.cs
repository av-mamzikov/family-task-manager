namespace FamilyTaskManager.Core.SpotAggregate.Events;

public sealed class SpotDeletedEvent : DomainEventBase
{
  public Guid SpotId { get; init; }
  public Guid FamilyId { get; init; }
  public string Name { get; init; } = string.Empty;
}
