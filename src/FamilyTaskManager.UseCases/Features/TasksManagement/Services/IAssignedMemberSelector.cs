namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public interface IAssignedMemberSelector
{
  ValueTask<FamilyMember?> SelectAssignedMemberAsync(TaskTemplate template, Spot spot,
    CancellationToken cancellationToken);
}
