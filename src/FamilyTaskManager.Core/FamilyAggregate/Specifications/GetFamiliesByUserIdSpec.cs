namespace FamilyTaskManager.Core.FamilyAggregate.Specifications;

public class GetFamiliesByUserIdSpec : Specification<Family>
{
  public GetFamiliesByUserIdSpec(Guid userId)
  {
    Query
      .Where(f => f.Members.Any(m => m.UserId == userId && m.IsActive))
      .Include(f => f.Members);
  }
}
