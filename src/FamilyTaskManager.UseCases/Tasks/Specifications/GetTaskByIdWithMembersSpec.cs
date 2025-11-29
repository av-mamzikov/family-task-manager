namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class GetTaskByIdWithMembersSpec : Specification<TaskInstance>
{
  public GetTaskByIdWithMembersSpec(Guid taskId)
  {
    Query
      .Where(t => t.Id == taskId)
      .Include(t => t.StartedByMember!)
      .ThenInclude(m => m.User);
  }
}
