namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class PetsByIdsSpec : Specification<Pet>
{
  public PetsByIdsSpec(IEnumerable<Guid> petIds)
  {
    var ids = petIds.ToList();
    if (ids.Count == 0)
    {
      Query.Where(p => false); // Return no results if no IDs provided
      return;
    }

    Query.Where(p => ids.Contains(p.Id));
  }
}
