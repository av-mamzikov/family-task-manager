namespace FamilyTaskManager.UseCases.Spots.Specifications;

public class GetSpotsByFamilySpec : Specification<SpotBowsing>
{
  public GetSpotsByFamilySpec(Guid familyId)
  {
    Query.Where(p => p.FamilyId == familyId);
  }
}
