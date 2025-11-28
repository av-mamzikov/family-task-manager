namespace FamilyTaskManager.UseCases.Tasks.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesByIdsSpec : Specification<TaskTemplate>
{
  public TaskTemplatesByIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query.Where(t => templateIds.Contains(t.Id));
  }
}
