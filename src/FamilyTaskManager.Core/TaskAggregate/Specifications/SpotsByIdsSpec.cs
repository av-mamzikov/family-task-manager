using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class SpotsByIdsSpec : Specification<Spot>
{
  public SpotsByIdsSpec(IEnumerable<Guid> spotIds)
  {
    var ids = spotIds.ToList();
    if (ids.Count == 0)
    {
      Query.Where(p => false); // Return no results if no IDs provided
      return;
    }

    Query.Where(p => ids.Contains(p.Id));
  }
}
