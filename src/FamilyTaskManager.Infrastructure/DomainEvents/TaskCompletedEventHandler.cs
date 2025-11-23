using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Notifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskCompletedEventHandler(
  ILogger<TaskCompletedEventHandler> logger,
  TelegramNotificationService notificationService,
  IRepository<User> userRepository)
  : INotificationHandler<TaskCompletedEvent>
{
  public async ValueTask Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Task completed: {TaskId} - {TaskTitle}",
      task.Id, task.Title);

    // Get user who completed the task
    if (task.CompletedBy.HasValue)
    {
      var user = await userRepository.GetByIdAsync(task.CompletedBy.Value, cancellationToken);
      if (user != null)
      {
        try
        {
          // Send notification to all family members
          await notificationService.SendTaskCompletedAsync(
            task.FamilyId,
            user.Name,
            task.Title,
            task.Points,
            cancellationToken);

          logger.LogInformation(
            "Task completion notification sent for task {TaskId} by user {UserName}",
            task.Id, user.Name);
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
