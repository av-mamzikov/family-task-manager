namespace FamilyTaskManager.Core.SpotAggregate.Specifications;

public class GetSpotWithFamilySpec : Specification<Spot>
{
  public GetSpotWithFamilySpec(Guid spotId)
  {
    Query
      .Where(p => p.Id == spotId)
      .Include(p => p.Family);
  }
}
