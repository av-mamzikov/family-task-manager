namespace FamilyTaskManager.Core.FamilyAggregate.Events;

public sealed class FamilyCreatedEvent(Family family) : DomainEventBase
{
  public Family Family { get; init; } = family;
}
