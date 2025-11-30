namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

/// <summary>
///   Specification to get TaskTemplate entities by their IDs.
/// </summary>
public class TaskTemplatesDtoByFamilyIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoByFamilyIdsSpec(Guid familyId)
  {
    Query.Where(t => t.FamilyId == familyId)
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
