namespace FamilyTaskManager.Core.FamilyAggregate.Events;

public sealed class MemberAddedEvent(Family family, FamilyMember member) : DomainEventBase
{
  public Family Family { get; init; } = family;
  public FamilyMember Member { get; init; } = member;
}
