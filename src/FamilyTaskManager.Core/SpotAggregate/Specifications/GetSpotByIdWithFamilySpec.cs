namespace FamilyTaskManager.Core.SpotAggregate.Specifications;

public class GetSpotByIdWithFamilySpec : Specification<Spot>
{
  public GetSpotByIdWithFamilySpec(Guid spotId)
  {
    Query
      .Where(p => p.Id == spotId)
      .Include(p => p.Family);
  }
}
