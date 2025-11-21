using FamilyTaskManager.Core.TaskAggregate.Events;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskCreatedEventHandler(ILogger<TaskCreatedEventHandler> logger) 
  : INotificationHandler<TaskCreatedEvent>
{
  public ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation("Task created: {TaskId} - {TaskTitle}", 
      notification.Task.Id, notification.Task.Title);
    return ValueTask.CompletedTask;
  }
}
