using FamilyTaskManager.Core.FamilyAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class FamilyCreatedEventHandler(ILogger<FamilyCreatedEventHandler> logger)
  : INotificationHandler<FamilyCreatedEvent>
{
  public ValueTask Handle(FamilyCreatedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Family created: {FamilyId} - {FamilyName}",
      notification.Family.Id, notification.Family.Name);
    return ValueTask.CompletedTask;
  }
}
