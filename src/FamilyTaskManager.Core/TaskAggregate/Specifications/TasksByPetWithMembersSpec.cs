namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksByPetWithMembersSpec : Specification<TaskInstance>
{
  public TasksByPetWithMembersSpec(Guid petId, TaskStatus? status = null)
  {
    Query.Where(t => t.PetId == petId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query
      .Include(t => t.StartedByMember!)
      .ThenInclude(m => m.User)
      .OrderBy(t => t.DueAt);
  }
}
