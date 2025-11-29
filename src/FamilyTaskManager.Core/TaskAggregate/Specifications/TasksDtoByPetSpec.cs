using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksDtoByPetSpec : Specification<TaskInstance, TaskDto>
{
  public TasksDtoByPetSpec(Guid petId, TaskStatus? status = null)
  {
    Query.Where(t => t.PetId == petId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
