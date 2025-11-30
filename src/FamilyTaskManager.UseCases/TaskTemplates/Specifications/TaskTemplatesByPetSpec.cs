namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class TaskTemplatesByPetSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesByPetSpec(Guid petId)
  {
    Query.Where(t => t.PetId == petId);
    Query.OrderBy(t => t.CreatedAt);
    Query.Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
