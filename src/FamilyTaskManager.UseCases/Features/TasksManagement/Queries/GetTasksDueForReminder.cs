using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UseCases.Features.TasksManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Queries;

public record GetTasksDueForReminderQuery(DateTime FromTime, DateTime ToTime) : IQuery<Result<List<TaskReminderDto>>>;

public class GetTasksDueForReminderHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetTasksDueForReminderQuery, Result<List<TaskReminderDto>>>
{
  public async ValueTask<Result<List<TaskReminderDto>>> Handle(GetTasksDueForReminderQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new TasksDueForReminderSpec(query.FromTime, query.ToTime);
    var tasks = await taskAppRepository.ListAsync(spec, cancellationToken);

    var result = new List<TaskReminderDto>();

    // Group tasks by family to minimize DB queries
    var tasksByFamily = tasks.GroupBy(t => t.FamilyId);

    foreach (var familyGroup in tasksByFamily)
    {
      var family = await familyAppRepository.GetByIdAsync(familyGroup.Key, cancellationToken);

      if (family == null) continue;

      var activeUserIds = family.Members
        .Where(m => m.IsActive)
        .Select(m => m.UserId)
        .ToList();

      foreach (var task in familyGroup)
        result.Add(new(
          task.Id,
          task.FamilyId,
          task.Title,
          task.DueAt,
          activeUserIds
        ));
    }

    return Result.Success(result);
  }
}
