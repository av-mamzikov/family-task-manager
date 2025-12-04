using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksDtoBySpotSpec : Specification<TaskInstance, TaskDto>
{
  public TasksDtoBySpotSpec(Guid spotId, TaskStatus? status = null)
  {
    Query.Where(t => t.SpotId == spotId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
