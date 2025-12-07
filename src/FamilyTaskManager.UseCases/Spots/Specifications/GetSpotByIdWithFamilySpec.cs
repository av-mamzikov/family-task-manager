namespace FamilyTaskManager.UseCases.Spots.Specifications;

public class GetSpotByIdWithFamilySpec : Specification<SpotBowsing>
{
  public GetSpotByIdWithFamilySpec(Guid SpotId)
  {
    Query
      .Where(p => p.Id == SpotId)
      .Include(p => p.Family);
  }
}
