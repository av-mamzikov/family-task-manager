using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class GetActiveTasksDtoByFamilySpec : Specification<TaskInstance, TaskDto>
{
  public GetActiveTasksDtoByFamilySpec(Guid familyId)
  {
    Query
      .Where(t => t.FamilyId == familyId &&
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress))
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
