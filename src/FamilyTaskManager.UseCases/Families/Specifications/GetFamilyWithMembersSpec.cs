namespace FamilyTaskManager.UseCases.Families.Specifications;

public class GetFamilyWithMembersSpec : Specification<Family>
{
  public GetFamilyWithMembersSpec(Guid familyId)
  {
    Query
      .Where(f => f.Id == familyId)
      .Include(f => f.Members).ThenInclude(m => m.User);
  }
}
