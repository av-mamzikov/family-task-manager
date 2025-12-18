namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class OverdueAssignedTasksSpec : Specification<TaskInstance>
{
  public OverdueAssignedTasksSpec(Guid familyId, DateTime utcNow)
  {
    Query
      .Where(t => t.FamilyId == familyId)
      .Where(t => t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress)
      .Where(t => t.AssignedToMemberId != null)
      .Where(t => t.DueAt < utcNow)
      .Include(t => t.AssignedToMember)
      .ThenInclude(m => m!.User);
  }
}
