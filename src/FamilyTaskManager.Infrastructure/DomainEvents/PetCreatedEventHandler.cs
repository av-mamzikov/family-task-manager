using FamilyTaskManager.Core.PetAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class PetCreatedEventHandler(
  ILogger<PetCreatedEventHandler> logger,
  ITelegramNotificationService notificationService)
  : INotificationHandler<PetCreatedEvent>
{
  public async ValueTask Handle(PetCreatedEvent notification, CancellationToken cancellationToken)
  {
    var pet = notification.Pet;

    logger.LogInformation(
      "Pet created: {PetId} - {PetName} ({PetType})",
      pet.Id, pet.Name, pet.Type);

    try
    {
      // Send notification to all family members
      await notificationService.SendPetCreatedAsync(
        pet.FamilyId,
        pet.Name,
        pet.Type.ToString(),
        cancellationToken);

      logger.LogInformation(
        "Pet creation notification sent for pet {PetId}: {PetName}",
        pet.Id, pet.Name);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet creation notification for pet {PetId}",
        pet.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
