using FamilyTaskManager.Core.Points;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class PointsCreditedEventHandler(ILogger<PointsCreditedEventHandler> logger) 
  : INotificationHandler<PointsCreditedEvent>
{
  public ValueTask Handle(PointsCreditedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Points credited: {Points} to member {MemberId} in family {FamilyId}", 
      notification.Points, notification.MemberId, notification.FamilyId);
    return ValueTask.CompletedTask;
  }
}
