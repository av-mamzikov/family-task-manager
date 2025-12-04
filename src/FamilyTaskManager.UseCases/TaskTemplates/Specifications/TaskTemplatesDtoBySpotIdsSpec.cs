using FamilyTaskManager.UseCases.Contracts.TaskTemplates;

namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesDtoBySpotIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoBySpotIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query.Where(t => templateIds.Contains(t.SpotId))
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
