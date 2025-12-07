namespace FamilyTaskManager.UseCases.Spots.Specifications;

public class GetSpotWithFamilySpec : Specification<SpotBowsing>
{
  public GetSpotWithFamilySpec(Guid SpotId)
  {
    Query
      .Where(p => p.Id == SpotId)
      .Include(p => p.Family);
  }
}
