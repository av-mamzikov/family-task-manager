using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class GetTaskDtoByIdWithMembersSpec : Specification<TaskInstance, TaskDto>
{
  public GetTaskDtoByIdWithMembersSpec(Guid taskId)
  {
    Query
      .Where(t => t.Id == taskId)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
