namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class ActiveTaskTemplatesSpec : Specification<TaskTemplate>
{
  public ActiveTaskTemplatesSpec()
  {
    Query.Where(t => t.IsActive);
  }
}
