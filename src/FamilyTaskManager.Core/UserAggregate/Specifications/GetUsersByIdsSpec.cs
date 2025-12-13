namespace FamilyTaskManager.Core.UserAggregate.Specifications;

public class GetUsersByIdsSpec : Specification<User>
{
  public GetUsersByIdsSpec(List<Guid> userIds)
  {
    Query.Where(u => userIds.Contains(u.Id));
  }
}
