namespace FamilyTaskManager.Core.FamilyAggregate.Events;

public sealed class FamilyCreatedEvent : DomainEventBase
{
  public required Guid FamilyId { get; init; }
}
