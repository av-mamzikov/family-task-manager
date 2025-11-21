namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public class GetActiveTasksByFamilySpec : Specification<TaskInstance>
{
  public GetActiveTasksByFamilySpec(Guid familyId)
  {
    Query
      .Where(t => t.FamilyId == familyId && 
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress))
      .OrderBy(t => t.DueAt);
  }
}
