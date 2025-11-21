namespace FamilyTaskManager.UseCases.Statistics.Specifications;

public class GetActionHistorySpec : Specification<ActionHistory>
{
  public GetActionHistorySpec(Guid familyId, Guid? userId, int? daysBack, int limit)
  {
    Query.Where(h => h.FamilyId == familyId);

    if (userId.HasValue)
    {
      Query.Where(h => h.UserId == userId.Value);
    }

    if (daysBack.HasValue)
    {
      var startDate = DateTime.UtcNow.AddDays(-daysBack.Value);
      Query.Where(h => h.CreatedAt >= startDate);
    }

    Query
      .OrderByDescending(h => h.CreatedAt)
      .Take(limit);
  }
}
