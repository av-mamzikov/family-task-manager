namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

public record TakeTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class TakeTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<TakeTaskCommand, Result>
{
  public async ValueTask<Result> Handle(TakeTaskCommand command, CancellationToken cancellationToken)
  {
    var task = await taskAppRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null) return Result.NotFound("Task not found");

    var family = await familyAppRepository.GetByIdAsync(task.FamilyId, cancellationToken);
    if (family == null) return Result.NotFound("Family not found");

    var startResult = task.StartByUserId(command.UserId, family);
    await taskAppRepository.UpdateAsync(task, cancellationToken);

    return startResult;
  }
}
