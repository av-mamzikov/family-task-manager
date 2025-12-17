using FamilyTaskManager.UseCases.Features.TasksManagement.Services;

namespace FamilyTaskManager.Infrastructure.Data.Queries;

public class TaskCompletionStatsQuery(AppDbContext dbContext) : ITaskCompletionStatsQuery
{
  public async ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCreatedAtByAssignedForTemplateAsync(
    Guid familyId,
    Guid templateId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken)
  {
    if (memberIds.Count == 0)
      return new Dictionary<Guid, DateTime>();

    var rows = await dbContext.TaskInstances
      .AsNoTracking()
      .Where(t => t.FamilyId == familyId)
      .Where(t => t.TemplateId == templateId)
      .Where(t => t.AssignedToMemberId != null)
      .Where(t => memberIds.Contains(t.AssignedToMemberId!.Value))
      .GroupBy(t => t.AssignedToMemberId!.Value)
      .Select(g => new { MemberId = g.Key, LastCompletedAt = g.Max(x => x.CreatedAt) })
      .ToListAsync(cancellationToken);

    return rows.ToDictionary(x => x.MemberId, x => x.LastCompletedAt);
  }

  public async ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCreatedAtByAssignedForSpotAsync(
    Guid familyId,
    Guid spotId,
    IReadOnlyCollection<Guid> memberIds,
    CancellationToken cancellationToken)
  {
    if (memberIds.Count == 0)
      return new Dictionary<Guid, DateTime>();

    var rows = await dbContext.TaskInstances
      .AsNoTracking()
      .Where(t => t.FamilyId == familyId)
      .Where(t => t.SpotId == spotId)
      .Where(t => t.AssignedToMemberId != null)
      .Where(t => memberIds.Contains(t.AssignedToMemberId!.Value))
      .GroupBy(t => t.AssignedToMemberId!.Value)
      .Select(g => new { MemberId = g.Key, LastCompletedAt = g.Max(x => x.CreatedAt) })
      .ToListAsync(cancellationToken);

    return rows.ToDictionary(x => x.MemberId, x => x.LastCompletedAt);
  }
}
