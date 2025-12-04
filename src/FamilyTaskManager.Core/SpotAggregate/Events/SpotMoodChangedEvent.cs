namespace FamilyTaskManager.Core.SpotAggregate.Events;

/// <summary>
///   Domain event raised when a Spot's mood changes significantly
/// </summary>
public sealed class SpotMoodChangedEvent : DomainEventBase
{
  public Guid SpotId { get; init; }
  public Guid FamilyId { get; init; }
  public string Name { get; init; } = string.Empty;
  public int OldMoodScore { get; init; }
  public int NewMoodScore { get; init; }
}
