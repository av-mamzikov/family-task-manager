using FamilyTaskManager.Core.PetAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class PetMoodUpdatedEventHandler(ILogger<PetMoodUpdatedEventHandler> logger) 
  : INotificationHandler<PetMoodUpdatedEvent>
{
  public ValueTask Handle(PetMoodUpdatedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Pet mood updated: {PetId} from {OldMood} to {NewMood}", 
      notification.Pet.Id, notification.PreviousMood, notification.NewMood);
    return ValueTask.CompletedTask;
  }
}
