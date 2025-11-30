namespace FamilyTaskManager.Core.PetAggregate.Events;

/// <summary>
///   Domain event raised when a pet's mood changes significantly
/// </summary>
public sealed class PetMoodChangedEvent(Pet pet, int oldMoodScore, int newMoodScore) : DomainEventBase
{
  public Pet Pet { get; init; } = pet;
  public int OldMoodScore { get; init; } = oldMoodScore;
  public int NewMoodScore { get; init; } = newMoodScore;
}
