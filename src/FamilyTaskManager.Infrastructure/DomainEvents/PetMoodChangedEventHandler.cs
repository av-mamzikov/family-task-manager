using FamilyTaskManager.Core.PetAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class PetMoodChangedEventHandler(
  ILogger<PetMoodChangedEventHandler> logger,
  ITelegramNotificationService notificationService)
  : INotificationHandler<PetMoodChangedEvent>
{
  public async ValueTask Handle(PetMoodChangedEvent notification, CancellationToken cancellationToken)
  {
    var pet = notification.Pet;

    logger.LogInformation(
      "Pet mood changed: {PetId} - {PetName} from {OldMood} to {NewMood}",
      pet.Id, pet.Name, notification.OldMoodScore, notification.NewMoodScore);

    try
    {
      // Send notification to all family members
      await notificationService.SendPetMoodChangedAsync(
        pet.FamilyId,
        pet.Name,
        notification.NewMoodScore,
        cancellationToken);

      logger.LogInformation(
        "Pet mood notification sent for pet {PetId}: {PetName}",
        pet.Id, pet.Name);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet mood notification for pet {PetId}",
        pet.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
