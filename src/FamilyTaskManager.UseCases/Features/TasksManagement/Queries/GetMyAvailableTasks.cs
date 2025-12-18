using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Queries;

public record GetMyAvailableTasksQuery(Guid FamilyId, Guid UserId) : IQuery<Result<List<TaskDto>>>;

public class GetMyAvailableTasksHandler(IAppRepository<TaskInstance> taskAppRepository)
  : IQueryHandler<GetMyAvailableTasksQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetMyAvailableTasksQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetUserAvailableTasksDtoBySpec(query.FamilyId, query.UserId);
    var tasks = await taskAppRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskDto>>.Success(tasks);
  }
}

public class GetUserAvailableTasksDtoBySpec : Specification<TaskInstance, TaskDto>
{
  public GetUserAvailableTasksDtoBySpec(Guid familyId, Guid userId)
  {
    Query
      .Where(t => t.FamilyId == familyId &&
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress) &&
                  (t.AssignedToMember == null || t.AssignedToMember.UserId == userId))
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
