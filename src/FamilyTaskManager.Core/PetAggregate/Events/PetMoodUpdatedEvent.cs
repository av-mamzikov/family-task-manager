namespace FamilyTaskManager.Core.PetAggregate.Events;

public sealed class PetMoodUpdatedEvent(Pet pet, int previousMood, int newMood) : DomainEventBase
{
  public Pet Pet { get; init; } = pet;
  public int PreviousMood { get; init; } = previousMood;
  public int NewMood { get; init; } = newMood;
}
