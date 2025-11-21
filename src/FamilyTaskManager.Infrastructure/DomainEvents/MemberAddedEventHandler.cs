using FamilyTaskManager.Core.FamilyAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Notifications;
using Ardalis.SharedKernel;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class MemberAddedEventHandler(
  ILogger<MemberAddedEventHandler> logger,
  TelegramNotificationService notificationService,
  IRepository<User> userRepository) 
  : INotificationHandler<MemberAddedEvent>
{
  public async ValueTask Handle(MemberAddedEvent notification, CancellationToken cancellationToken)
  {
    var family = notification.Family;
    var member = notification.Member;
    
    logger.LogInformation("Member added to family {FamilyId}: {MemberId}", 
      family.Id, member.Id);

    // Get user info
    var user = await userRepository.GetByIdAsync(member.UserId, cancellationToken);
    if (user != null)
    {
      try
      {
        // Send notification to all family members
        await notificationService.SendMemberJoinedAsync(
          family.Id,
          user.Name,
          cancellationToken);
        
        logger.LogInformation(
          "Member joined notification sent for family {FamilyId}: user {UserName}",
          family.Id, user.Name);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, 
          "Failed to send member joined notification for family {FamilyId}",
          family.Id);
        // Don't throw - notification failure shouldn't break the flow
      }
    }
  }
}
