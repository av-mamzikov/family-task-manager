using FamilyTaskManager.Core.PetAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class PetDeletedEventHandler(
  ILogger<PetDeletedEventHandler> logger,
  ITelegramNotificationService notificationService)
  : INotificationHandler<PetDeletedEvent>
{
  public async ValueTask Handle(PetDeletedEvent notification, CancellationToken cancellationToken)
  {
    var pet = notification.Pet;

    logger.LogInformation(
      "Pet deleted: {PetId} - {PetName}",
      pet.Id, pet.Name);

    try
    {
      // Send notification to all family members
      await notificationService.SendPetDeletedAsync(
        pet.FamilyId,
        pet.Name,
        cancellationToken);

      logger.LogInformation(
        "Pet deletion notification sent for pet {PetId}: {PetName}",
        pet.Id, pet.Name);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet deletion notification for pet {PetId}",
        pet.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
