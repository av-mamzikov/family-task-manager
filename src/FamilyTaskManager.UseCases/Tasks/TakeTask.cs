namespace FamilyTaskManager.UseCases.Tasks;

public record TakeTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class TakeTaskHandler(IRepository<TaskInstance> taskRepository)
  : ICommandHandler<TakeTaskCommand, Result>
{
  public async ValueTask<Result> Handle(TakeTaskCommand command, CancellationToken cancellationToken)
  {
    var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null)
    {
      return Result.NotFound("Task not found");
    }

    if (task.Status != TaskStatus.Active)
    {
      return Result.Error("Task is not available");
    }

    task.Start();
    await taskRepository.UpdateAsync(task, cancellationToken);

    return Result.Success();
  }
}
