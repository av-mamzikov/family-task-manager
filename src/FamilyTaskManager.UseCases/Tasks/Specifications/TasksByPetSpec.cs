namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class TasksByPetSpec : Specification<TaskInstance>
{
  public TasksByPetSpec(Guid petId)
  {
    Query.Where(t => t.PetId == petId);
  }
}
