namespace FamilyTaskManager.UseCases.Tasks;

public record CancelTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class CancelTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<CancelTaskCommand, Result>
{
  public async ValueTask<Result> Handle(CancelTaskCommand command, CancellationToken cancellationToken)
  {
    var task = await taskAppRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null) return Result.NotFound("Task not found");

    if (task.Status != TaskStatus.InProgress) return Result.Error("Task is not in progress");

    // Get family with members to validate user belongs to family
    var familySpec = new GetFamilyWithMembersSpec(task.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
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
