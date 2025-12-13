namespace FamilyTaskManager.Core.SpotAggregate.Specifications;

public class GetSpotsByResponsibleMemberSpec : Specification<Spot>
{
  public GetSpotsByResponsibleMemberSpec(Guid familyMemberId)
  {
    Query
      .Where(s => s.ResponsibleMembers.Any(m => m.Id == familyMemberId));
  }
}
