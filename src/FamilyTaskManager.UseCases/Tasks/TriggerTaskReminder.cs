using FamilyTaskManager.Core.TaskAggregate;
using Ardalis.SharedKernel;

namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
/// Command to trigger a reminder for a specific task
/// </summary>
public record TriggerTaskReminderCommand(Guid TaskId) : ICommand<Result>;

public class TriggerTaskReminderHandler(IRepository<TaskInstance> taskRepository) 
  : ICommandHandler<TriggerTaskReminderCommand, Result>
{
  public async ValueTask<Result> Handle(TriggerTaskReminderCommand command, CancellationToken cancellationToken)
  {
    var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
    
    if (task == null)
    {
      return Result.NotFound($"Task with ID {command.TaskId} not found");
    }

    // Domain registers TaskReminderDueEvent
    task.TriggerReminder();
    
    // Save changes (will dispatch domain events)
    await taskRepository.UpdateAsync(task, cancellationToken);

    return Result.Success();
  }
}
