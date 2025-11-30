namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesWithFamilyByIdsSpec : Specification<TaskTemplate>
{
  public TaskTemplatesWithFamilyByIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query.Where(t => templateIds.Contains(t.Id)).Include(t => t.Family);
  }
}
