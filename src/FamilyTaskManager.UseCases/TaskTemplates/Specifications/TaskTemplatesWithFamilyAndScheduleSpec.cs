namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesWithFamilyAndScheduleSpec : Specification<TaskTemplate>
{
  public TaskTemplatesWithFamilyAndScheduleSpec()
  {
    Query.Include(t => t.Family);
  }
}
