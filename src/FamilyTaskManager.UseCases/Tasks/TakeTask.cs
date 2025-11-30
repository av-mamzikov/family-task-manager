namespace FamilyTaskManager.UseCases.Tasks;

public record TakeTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class TakeTaskHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Family> familyRepository) : ICommandHandler<TakeTaskCommand, Result>
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

    // Get family with members to validate user belongs to family
    var familySpec = new GetFamilyWithMembersSpec(task.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Family not found");
    }

    // Find member
    var member = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.IsActive);
    if (member == null)
    {
      return Result.Error("User is not a member of this family");
    }

    task.Start(member.Id);
    await taskRepository.UpdateAsync(task, cancellationToken);

    return Result.Success();
  }
}
