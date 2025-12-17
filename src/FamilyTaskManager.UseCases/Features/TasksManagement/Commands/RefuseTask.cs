namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

public record RefuseTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class RefuseTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<RefuseTaskCommand, Result>
{
  public async ValueTask<Result> Handle(RefuseTaskCommand command, CancellationToken cancellationToken)
  {
    var task = await taskAppRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null) return Result.NotFound("Task not found");

    if (task.Status != TaskStatus.InProgress) return Result.Error("Task is not in progress");

    // Get family with members to validate user belongs to family
    var family = await familyAppRepository.GetByIdAsync(task.FamilyId, cancellationToken);
    if (family == null) return Result.NotFound("Family not found");

    // Find member
    var member = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.IsActive);
    if (member == null) return Result.Error("User is not a member of this family");

    // Check if current user is the one who started the task
    if (task.StartedByMemberId.HasValue && task.StartedByMemberId.Value != member.Id)
      return Result.Error("Only the user who started this task can cancel it");

    task.Release();
    await taskAppRepository.UpdateAsync(task, cancellationToken);

    return Result.Success();
  }
}
