namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class CompletedTaskInstancesByTemplateAndFamilySpec : Specification<TaskInstance>
{
  public CompletedTaskInstancesByTemplateAndFamilySpec(Guid familyId, Guid templateId)
  {
    Query
      .Where(t => t.FamilyId == familyId)
      .Where(t => t.TemplateId == templateId)
      .Where(t => t.Status == TaskStatus.Completed)
      .Where(t => t.AssignedToMemberId != null && t.CompletedAt != null)
      .Include(t => t.AssignedToMember)
      .ThenInclude(m => m!.User);
  }
}
