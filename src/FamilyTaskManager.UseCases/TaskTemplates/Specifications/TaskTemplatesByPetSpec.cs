namespace FamilyTaskManager.UseCases.TaskTemplates.Specifications;

public class TaskTemplatesByPetSpec : Specification<TaskTemplate>
{
  public TaskTemplatesByPetSpec(Guid petId, bool? isActive = null)
  {
    Query.Where(t => t.PetId == petId);

    if (isActive.HasValue)
    {
      Query.Where(t => t.IsActive == isActive.Value);
    }

    Query.OrderBy(t => t.CreatedAt);
  }
}
