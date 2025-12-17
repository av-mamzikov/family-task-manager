namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public interface ITaskCompletionStatsQuery
{
  ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCreatedAtByAssignedForTemplateAsync(
    Guid familyId,
    Guid templateId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken);

  ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCreatedAtByAssignedForSpotAsync(
    Guid familyId,
    Guid spotId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken);
}
