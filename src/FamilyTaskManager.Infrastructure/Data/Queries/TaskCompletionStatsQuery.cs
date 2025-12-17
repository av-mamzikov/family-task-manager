using FamilyTaskManager.UseCases.Features.TasksManagement.Services;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Infrastructure.Data.Queries;

public class TaskCompletionStatsQuery(AppDbContext dbContext) : ITaskCompletionStatsQuery
{
  public async ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCompletedAtByMemberForTemplateAsync(
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
      .Where(t => t.Status == TaskStatus.Completed)
      .Where(t => t.CompletedByMemberId != null && t.CompletedAt != null)
      .Where(t => memberIds.Contains(t.CompletedByMemberId!.Value))
      .GroupBy(t => t.CompletedByMemberId!.Value)
      .Select(g => new { MemberId = g.Key, LastCompletedAt = g.Max(x => x.CompletedAt!.Value) })
      .ToListAsync(cancellationToken);

    return rows.ToDictionary(x => x.MemberId, x => x.LastCompletedAt);
  }

  public async ValueTask<IReadOnlyDictionary<Guid, DateTime>> GetLastCompletedAtByMemberForSpotAsync(
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
      .Where(t => t.Status == TaskStatus.Completed)
      .Where(t => t.CompletedByMemberId != null && t.CompletedAt != null)
      .Where(t => memberIds.Contains(t.CompletedByMemberId!.Value))
      .GroupBy(t => t.CompletedByMemberId!.Value)
      .Select(g => new { MemberId = g.Key, LastCompletedAt = g.Max(x => x.CompletedAt!.Value) })
      .ToListAsync(cancellationToken);

    return rows.ToDictionary(x => x.MemberId, x => x.LastCompletedAt);
  }
}
