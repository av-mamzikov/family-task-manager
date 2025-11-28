namespace FamilyTaskManager.Core.PetAggregate.Events;

public sealed class PetCreatedEvent(Pet pet) : DomainEventBase
{
  public Pet Pet { get; init; } = pet;
}
