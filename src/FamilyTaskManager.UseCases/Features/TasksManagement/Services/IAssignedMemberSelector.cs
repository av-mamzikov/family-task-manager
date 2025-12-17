namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public interface IAssignedMemberSelector
{
  ValueTask<Guid?> SelectAssignedMemberIdAsync(TaskTemplate template, Spot spot,
    CancellationToken cancellationToken);
}
