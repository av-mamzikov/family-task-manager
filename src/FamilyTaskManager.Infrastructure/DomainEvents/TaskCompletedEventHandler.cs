using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskCompletedEventHandler(ILogger<TaskCompletedEventHandler> logger) 
  : INotificationHandler<TaskCompletedEvent>
{
  public ValueTask Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Task completed: {TaskId} - {TaskTitle}", 
      notification.Task.Id, notification.Task.Title);
    return ValueTask.CompletedTask;
  }
}
