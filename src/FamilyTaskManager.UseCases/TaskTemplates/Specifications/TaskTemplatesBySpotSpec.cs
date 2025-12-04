namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class TaskTemplatesBySpotSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesBySpotSpec(Guid SpotId)
  {
    Query.Where(t => t.SpotId == SpotId);
    Query.OrderBy(t => t.CreatedAt);
    Query.Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
