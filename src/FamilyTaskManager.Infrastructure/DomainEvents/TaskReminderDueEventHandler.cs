using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Notifications;
using Ardalis.SharedKernel;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskReminderDueEventHandler(
  ILogger<TaskReminderDueEventHandler> logger) 
  : INotificationHandler<TaskReminderDueEvent>
{
  public ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;
    
    logger.LogInformation("Task reminder due: {TaskId} - {TaskTitle}", 
      task.Id, task.Title);

    // TODO: Implement actual notification sending
    // Need to get family members and their telegram IDs
    // This will be implemented when we refactor the notification system
    
    logger.LogInformation(
      "Task reminder event handled for task {TaskId}",
      task.Id);
    
    return ValueTask.CompletedTask;
  }
}
