namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksBySpotSpec : Specification<TaskInstance>
{
  public TasksBySpotSpec(Guid SpotId, TaskStatus? status = null)
  {
    Query.Where(t => t.SpotId == SpotId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query.OrderBy(t => t.DueAt);
  }
}
