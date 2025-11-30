namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksByPetWithFamilySpec : Specification<TaskInstance>
{
  public TasksByPetWithFamilySpec(Guid petId, TaskStatus? status = null)
  {
    Query.Where(t => t.PetId == petId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query
      .Include(t => t.Pet)
      .Include(t => t.Family)
      .OrderBy(t => t.DueAt);
  }
}
