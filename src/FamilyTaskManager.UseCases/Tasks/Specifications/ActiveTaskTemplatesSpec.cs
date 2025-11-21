namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class ActiveTaskTemplatesSpec : Specification<TaskTemplate>
{
  public ActiveTaskTemplatesSpec()
  {
    Query.Where(t => t.IsActive);
  }
}
