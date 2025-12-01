using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.Specifications;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskStartedEventHandler(
  ILogger<TaskStartedEventHandler> logger,
  ITelegramNotificationService notificationService,
  IReadOnlyEntityRepository<FamilyMember> memberRepository)
  : INotificationHandler<TaskStartedEvent>
{
  public async ValueTask Handle(TaskStartedEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Task started: {TaskId} - {TaskTitle}",
      task.Id, task.Title);

    // Get member who started the task
    if (task.StartedByMemberId.HasValue)
    {
      var memberSpec = new GetFamilyMemberByIdWithUserSpec(task.StartedByMemberId.Value);
      var member = await memberRepository.FirstOrDefaultAsync(memberSpec, cancellationToken);
      if (member?.User != null)
        try
        {
          // Send notification to all family members
          await notificationService.SendTaskStartedAsync(
            task.FamilyId,
            member.UserId,
            member.User.Name,
            task.Title,
            task.Points,
            cancellationToken);

          logger.LogInformation(
            "Task started notification sent for task {TaskId} by user {UserName}",
            task.Id, member.User.Name);
        }
        catch (Exception ex)
        {
          logger.LogError(ex,
            "Failed to send task started notification for task {TaskId}",
            task.Id);
          // Don't throw - notification failure shouldn't break the flow
        }
    }
  }
}
