using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Tasks;

public record DeleteTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class DeleteTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<DeleteTaskCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteTaskCommand command, CancellationToken cancellationToken)
  {
    // Get task with Spot included (needed for TaskDeletedEvent)
    var taskSpec = new GetTaskByIdWithSpotSpec(command.TaskId);
    var task = await taskAppRepository.FirstOrDefaultAsync(taskSpec, cancellationToken);
    if (task == null) return Result.NotFound("Task not found");

    // Get family with members to validate user belongs to family and check permissions
    var familySpec = new GetFamilyWithMembersAndUsersSpec(task.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
    if (family == null) return Result.NotFound("Family not found");

    // Find member
    var member = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.IsActive);
    if (member == null) return Result.Error("User is not a member of this family");

    // Check permissions: Only admins or the member who started the task can delete
    // Admins can delete any task, regular users can only delete tasks they started
    if (member.Role != FamilyRole.Admin)
      if (!task.StartedByMemberId.HasValue || task.StartedByMemberId.Value != member.Id)
        return Result.Error("Only admins or the user who started this task can delete it");

    // Delete the task (domain event will be registered automatically)
    task.Delete(member);

    // Hard delete the task from repository
    await taskAppRepository.DeleteAsync(task, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
