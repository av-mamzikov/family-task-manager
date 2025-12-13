namespace FamilyTaskManager.Core.SpotAggregate.Specifications;

public class GetSpotWithFamilySpec : Specification<Spot>
{
  public GetSpotWithFamilySpec(Guid SpotId)
  {
    Query
      .Where(p => p.Id == SpotId)
      .Include(p => p.Family);
  }
}
