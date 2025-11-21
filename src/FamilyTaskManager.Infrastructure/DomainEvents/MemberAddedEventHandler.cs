using FamilyTaskManager.Core.FamilyAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class MemberAddedEventHandler(ILogger<MemberAddedEventHandler> logger) 
  : INotificationHandler<MemberAddedEvent>
{
  public ValueTask Handle(MemberAddedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Member added to family {FamilyId}: {MemberId}", 
      notification.Family.Id, notification.Member.Id);
    return ValueTask.CompletedTask;
  }
}
