using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.Specifications;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskCompletedEventHandler(
  ILogger<TaskCompletedEventHandler> logger,
  ITelegramNotificationService notificationService,
  IReadOnlyEntityRepository<FamilyMember> memberRepository)
  : INotificationHandler<TaskCompletedEvent>
{
  public async ValueTask Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Task completed: {TaskId} - {TaskTitle}",
      task.Id, task.Title);

    // Get member who completed the task
    if (task.CompletedByMemberId.HasValue)
    {
      var memberSpec = new GetFamilyMemberByIdWithUserSpec(task.CompletedByMemberId.Value);
      var member = await memberRepository.FirstOrDefaultAsync(memberSpec, cancellationToken);
      if (member?.User != null)
      {
        try
        {
          // Send notification to all family members
          await notificationService.SendTaskCompletedAsync(
            task.FamilyId,
            member.User.Name,
            task.Title,
            task.Points,
            cancellationToken);

          logger.LogInformation(
            "Task completion notification sent for task {TaskId} by user {UserName}",
            task.Id, member.User.Name);
        }
        catch (Exception ex)
        {
          logger.LogError(ex,
            "Failed to send task completion notification for task {TaskId}",
            task.Id);
          // Don't throw - notification failure shouldn't break the flow
        }
      }
    }
  }
}
