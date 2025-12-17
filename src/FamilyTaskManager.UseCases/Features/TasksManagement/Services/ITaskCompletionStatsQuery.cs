namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public interface ITaskCompletionStatsQuery
{
  ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCompletedAtByMemberForTemplateAsync(
    Guid familyId,
    Guid templateId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken);

  ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCompletedAtByMemberForSpotAsync(
    Guid familyId,
    Guid spotId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken);
}
