namespace FamilyTaskManager.UseCases.Tasks;

public record TaskReminderDto(
  Guid TaskId,
  Guid FamilyId,
  string Title,
  DateTime DueAt,
  List<Guid> FamilyMemberIds);

public record GetTasksDueForReminderQuery(DateTime FromTime, DateTime ToTime) : IQuery<Result<List<TaskReminderDto>>>;

public class GetTasksDueForReminderHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Family> familyRepository) 
  : IQueryHandler<GetTasksDueForReminderQuery, Result<List<TaskReminderDto>>>
{
  public async ValueTask<Result<List<TaskReminderDto>>> Handle(GetTasksDueForReminderQuery query, CancellationToken cancellationToken)
  {
    var spec = new TasksDueForReminderSpec(query.FromTime, query.ToTime);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    var result = new List<TaskReminderDto>();

    // Group tasks by family to minimize DB queries
    var tasksByFamily = tasks.GroupBy(t => t.FamilyId);

    foreach (var familyGroup in tasksByFamily)
    {
      // Get family with members
      var familySpec = new GetFamilyWithMembersSpec(familyGroup.Key);
      var family = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
      
      if (family == null) continue;

      var activeUserIds = family.Members
        .Where(m => m.IsActive)
        .Select(m => m.UserId)
        .ToList();

      foreach (var task in familyGroup)
      {
        result.Add(new TaskReminderDto(
          task.Id,
          task.FamilyId,
          task.Title,
          task.DueAt,
          activeUserIds
        ));
      }
    }

    return Result.Success(result);
  }
}
