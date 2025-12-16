namespace FamilyTaskManager.Core.TaskAggregate.Specifications;

public class TaskTemplateResponsibleMembersSpec : Specification<TaskTemplate>
{
  public TaskTemplateResponsibleMembersSpec(Guid taskTemplateId)
  {
    Query
      .Where(t => t.Id == taskTemplateId)
      .Include(t => t.ResponsibleMembers);
  }
}