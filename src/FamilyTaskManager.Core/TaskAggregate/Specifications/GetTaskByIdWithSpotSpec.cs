namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class GetTaskByIdWithSpotSpec : Specification<TaskInstance>
{
  public GetTaskByIdWithSpotSpec(Guid taskId)
  {
    Query
      .Where(t => t.Id == taskId)
      .Include(t => t.Spot);
  }
}
