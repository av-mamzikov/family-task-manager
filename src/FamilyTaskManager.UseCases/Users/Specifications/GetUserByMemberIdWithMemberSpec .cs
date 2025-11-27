namespace FamilyTaskManager.UseCases.Users.Specifications;

public class GetUserByMemberIdWithMemberSpec : Specification<User>
{
  public GetUserByMemberIdWithMemberSpec(Guid memberId)
  {
    Query.Where(u => u.FamilyMembers.Any(m => m.Id == memberId)).Include(u => u.FamilyMembers);
  }
}
