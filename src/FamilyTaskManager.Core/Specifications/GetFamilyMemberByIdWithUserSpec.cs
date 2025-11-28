using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.Core.Specifications;

public class GetFamilyMemberByIdWithUserSpec(Guid id) : Specification<FamilyMember>
{
  public void Configure() =>
    Query.Where(m => m.Id == id)
      .Include(m => m.User);
}
