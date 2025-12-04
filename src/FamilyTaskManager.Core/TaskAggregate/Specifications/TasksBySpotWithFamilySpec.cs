namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksBySpotWithFamilySpec : Specification<TaskInstance>
{
  public TasksBySpotWithFamilySpec(Guid spotId, TaskStatus? status = null)
  {
    Query.Where(t => t.SpotId == spotId);

    if (status.HasValue) Query.Where(t => t.Status == status.Value);

    Query
      .Include(t => t.Spot)
      .Include(t => t.Family)
      .OrderBy(t => t.DueAt);
  }
}
