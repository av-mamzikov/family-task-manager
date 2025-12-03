namespace FamilyTaskManager.UseCases.Families.Specifications;

public class GetFamilyWithMembersAndUsersSpec : Specification<Family>
{
  public GetFamilyWithMembersAndUsersSpec(Guid familyId)
  {
    Query
      .Where(f => f.Id == familyId)
      .Include(f => f.Members)
      .ThenInclude(m => m.User);
  }
}
