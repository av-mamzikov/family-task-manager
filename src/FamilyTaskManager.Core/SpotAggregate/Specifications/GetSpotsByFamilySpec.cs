namespace FamilyTaskManager.Core.SpotAggregate.Specifications;

public class GetSpotsByFamilySpec : Specification<Spot>
{
  public GetSpotsByFamilySpec(Guid familyId)
  {
    Query.Where(p => p.FamilyId == familyId);
  }
}
