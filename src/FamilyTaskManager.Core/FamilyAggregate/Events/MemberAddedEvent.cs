namespace FamilyTaskManager.Core.FamilyAggregate.Events;

public sealed class MemberAddedEvent : DomainEventBase
{
  public Guid FamilyId { get; init; }
  public Guid MemberId { get; init; }
  public Guid UserId { get; init; }
  public string UserName { get; init; } = string.Empty;
}
