namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

/// <summary>
///   Command to trigger a reminder for a specific task
/// </summary>
public record TriggerTaskReminderCommand(Guid TaskId) : ICommand<Result>;

public class TriggerTaskReminderHandler(IAppRepository<TaskInstance> taskAppRepository)
  : ICommandHandler<TriggerTaskReminderCommand, Result>
{
  public async ValueTask<Result> Handle(TriggerTaskReminderCommand command, CancellationToken cancellationToken)
  {
    var task = await taskAppRepository.GetByIdAsync(command.TaskId, cancellationToken);

    if (task == null) return Result.NotFound($"Task with ID {command.TaskId} not found");

    // Domain registers TaskReminderDueEvent
    task.TriggerReminder();

    // Save changes (will dispatch domain events)
    await taskAppRepository.UpdateAsync(task, cancellationToken);

    return Result.Success();
  }
}
