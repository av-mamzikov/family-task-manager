namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TaskInstancesByTemplateSpec : Specification<TaskInstance>
{
  public TaskInstancesByTemplateSpec(Guid templateId)
  {
    Query.Where(t => t.TemplateId == templateId);
  }
}
