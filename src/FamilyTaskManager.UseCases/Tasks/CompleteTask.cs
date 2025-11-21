namespace FamilyTaskManager.UseCases.Tasks;

public record CompleteTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class CompleteTaskHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Family> familyRepository) : ICommandHandler<CompleteTaskCommand, Result>
{
  public async ValueTask<Result> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
  {
    // Get task
    var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null)
    {
      return Result.NotFound("Task not found");
    }

    if (task.Status == TaskStatus.Completed)
    {
      return Result.Error("Task is already completed");
    }

    // Get family with members
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

    // Complete task
    task.Complete(command.UserId, DateTime.UtcNow);
    
    // Add points to member
    member.AddPoints(task.Points);

    // Update both entities
    await taskRepository.UpdateAsync(task, cancellationToken);
    await familyRepository.UpdateAsync(family, cancellationToken);

    return Result.Success();
  }
}
