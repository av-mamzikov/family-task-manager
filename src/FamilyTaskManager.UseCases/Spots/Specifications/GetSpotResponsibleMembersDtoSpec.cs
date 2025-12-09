using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Spots.Specifications;

public class GetSpotResponsibleMembersDtoSpec : Specification<FamilyMember, FamilyMemberDto>
{
  public GetSpotResponsibleMembersDtoSpec(Guid spotId)
  {
    Query
      .Where(m => m.IsActive && m.ResponsibleSpots.Any(s => s.Id == spotId))
      .Select(FamilyMemberDto.Projections.FromFamilyMember);
  }
}
