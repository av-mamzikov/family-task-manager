namespace FamilyTaskManager.Core.Points;

public sealed class PointsCreditedEvent : DomainEventBase
{
  public required Guid FamilyId { get; init; }
  public required Guid MemberId { get; init; }
  public required int Points { get; init; }
  public required Guid TaskId { get; init; }
}
