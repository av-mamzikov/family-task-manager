namespace FamilyTaskManager.Core.PetAggregate.Events;

/// <summary>
///   Domain event raised when a pet's mood changes significantly
/// </summary>
public sealed class PetMoodChangedEvent : DomainEventBase
{
  public Guid PetId { get; init; }
  public Guid FamilyId { get; init; }
  public string Name { get; init; } = string.Empty;
  public int OldMoodScore { get; init; }
  public int NewMoodScore { get; init; }
}
