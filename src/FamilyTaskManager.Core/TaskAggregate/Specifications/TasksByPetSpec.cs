namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksByPetSpec : Specification<TaskInstance>
{
  public TasksByPetSpec(Guid petId, TaskStatus? status = null)
  {
    Query.Where(t => t.PetId == petId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query.OrderBy(t => t.DueAt);
  }
}
