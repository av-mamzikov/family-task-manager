namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class SpotsByIdsSpec : Specification<Spot>
{
  public SpotsByIdsSpec(IEnumerable<Guid> SpotIds)
  {
    var ids = SpotIds.ToList();
    if (ids.Count == 0)
    {
      Query.Where(p => false); // Return no results if no IDs provided
      return;
    }

    Query.Where(p => ids.Contains(p.Id));
  }
}
