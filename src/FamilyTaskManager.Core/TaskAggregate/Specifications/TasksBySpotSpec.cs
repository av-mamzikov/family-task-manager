namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksBySpotSpec : Specification<TaskInstance>
{
  public TasksBySpotSpec(Guid spotId, TaskStatus? status = null)
  {
    Query.Where(t => t.SpotId == spotId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query.OrderBy(t => t.DueAt);
  }
}
