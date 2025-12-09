using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesDtoByIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoByIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query
      .Where(t => templateIds.Contains(t.Id))
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
