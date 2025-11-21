namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
/// Command to send reminders for all tasks that are due soon
/// </summary>
public record SendTaskRemindersCommand : ICommand<Result>;

public class SendTaskRemindersHandler(IMediator mediator) 
  : ICommandHandler<SendTaskRemindersCommand, Result>
{
  public async ValueTask<Result> Handle(SendTaskRemindersCommand command, CancellationToken cancellationToken)
  {
    // Get all tasks that need reminders (due within next hour)
    var now = DateTime.UtcNow;
    var oneHourFromNow = now.AddHours(1);
    
    var tasksResult = await mediator.Send(
      new GetTasksDueForReminderQuery(now, oneHourFromNow), 
      cancellationToken);
    
    if (!tasksResult.IsSuccess)
    {
      return Result.Error("Failed to retrieve tasks for reminders");
    }

    var tasks = tasksResult.Value;
    
    // Trigger reminder for each task (will register domain events)
    foreach (var task in tasks)
    {
      await mediator.Send(new TriggerTaskReminderCommand(task.TaskId), cancellationToken);
    }

    return Result.Success();
  }
}
