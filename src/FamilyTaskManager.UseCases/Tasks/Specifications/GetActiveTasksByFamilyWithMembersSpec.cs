namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class GetActiveTasksByFamilyWithMembersSpec : Specification<TaskInstance>
{
  public GetActiveTasksByFamilyWithMembersSpec(Guid familyId)
  {
    Query
      .Where(t => t.FamilyId == familyId &&
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress))
      .Include(t => t.StartedByMember!)
      .ThenInclude(m => m.User)
      .OrderBy(t => t.DueAt);
  }
}
