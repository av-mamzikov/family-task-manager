namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class TaskTemplatesByFamilySpec : Specification<TaskTemplate>
{
  public TaskTemplatesByFamilySpec(Guid familyId, bool? isActive = null)
  {
    Query.Where(t => t.FamilyId == familyId);

    if (isActive.HasValue)
    {
      Query.Where(t => t.IsActive == isActive.Value);
    }

    Query.OrderBy(t => t.CreatedAt);
  }
}
