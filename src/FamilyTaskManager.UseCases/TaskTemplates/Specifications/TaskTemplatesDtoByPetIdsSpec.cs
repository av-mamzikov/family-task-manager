namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesDtoByPetIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoByPetIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query.Where(t => templateIds.Contains(t.PetId))
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
