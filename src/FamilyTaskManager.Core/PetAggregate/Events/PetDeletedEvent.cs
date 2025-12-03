namespace FamilyTaskManager.Core.PetAggregate.Events;

public sealed class PetDeletedEvent : DomainEventBase
{
  public Guid PetId { get; init; }
  public Guid FamilyId { get; init; }
  public string Name { get; init; } = string.Empty;
}
