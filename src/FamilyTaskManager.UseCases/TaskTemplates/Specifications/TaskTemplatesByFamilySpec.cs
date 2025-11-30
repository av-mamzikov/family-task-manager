namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class TaskTemplatesByFamilySpec : Specification<TaskTemplate>
{
  public TaskTemplatesByFamilySpec(Guid familyId)
  {
    Query.Where(t => t.FamilyId == familyId);
    Query.OrderBy(t => t.CreatedAt);
  }
}
