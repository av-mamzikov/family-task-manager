namespace FamilyTaskManager.Core.Points;

public sealed class PointsCreditedEvent(Guid familyId, Guid memberId, int points, Guid taskId) : DomainEventBase
{
  public Guid FamilyId { get; init; } = familyId;
  public Guid MemberId { get; init; } = memberId;
  public int Points { get; init; } = points;
  public Guid TaskId { get; init; } = taskId;
}
