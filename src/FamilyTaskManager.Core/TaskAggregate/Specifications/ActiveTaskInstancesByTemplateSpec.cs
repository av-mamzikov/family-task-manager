namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class ActiveTaskInstancesByTemplateSpec : Specification<TaskInstance>
{
  public ActiveTaskInstancesByTemplateSpec(Guid templateId)
  {
    Query
      .Where(t => t.TemplateId == templateId)
      .Where(t => t.CompletedAt == null)
      .Where(t => t.Status != TaskStatus.Completed);
  }
}
