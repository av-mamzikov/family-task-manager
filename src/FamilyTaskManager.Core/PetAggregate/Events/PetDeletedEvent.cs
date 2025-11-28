namespace FamilyTaskManager.Core.PetAggregate.Events;

public sealed class PetDeletedEvent(Pet pet) : DomainEventBase
{
  public Pet Pet { get; init; } = pet;
}
