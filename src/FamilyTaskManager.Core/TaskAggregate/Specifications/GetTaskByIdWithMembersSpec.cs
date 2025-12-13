using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class GetTaskByIdWithMembersSpec : Specification<TaskInstance, TaskDto>
{
  public GetTaskByIdWithMembersSpec(Guid taskId)
  {
    Query
      .Where(t => t.Id == taskId)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
