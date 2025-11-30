using FamilyTaskManager.UseCases.Families;

namespace FamilyTaskManager.UseCases.Users.Specifications;

public class GetFamilyMemberDtoByIdSpec : Specification<FamilyMember, FamilyMemberDto>
{
  public GetFamilyMemberDtoByIdSpec(Guid memberId)
  {
    Query
      .Where(m => m.Id == memberId)
      .Select(FamilyMemberDto.Projections.FromFamilyMember);
  }
}
