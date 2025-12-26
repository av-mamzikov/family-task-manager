namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TasksDueForReminderSpec : Specification<TaskInstance>
{
  public TasksDueForReminderSpec(DateTime fromTime, DateTime toTime)
  {
    Query
      .Where(t => t.Status == TaskStatus.Active ||
                  t.Status == TaskStatus.InProgress)
      .Where(t => t.DueAt >= fromTime && t.DueAt < toTime)
      .OrderBy(t => t.DueAt);
  }
}
