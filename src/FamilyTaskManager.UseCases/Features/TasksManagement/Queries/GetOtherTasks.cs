using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Queries;

public record GetOtherTasksQuery(Guid FamilyId, Guid UserId) : IQuery<Result<List<TaskDto>>>;

public class GetOtherTasksHandler(IAppRepository<TaskInstance> taskAppRepository)
  : IQueryHandler<GetOtherTasksQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetOtherTasksQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetOtherTasksDtoBySpec(query.FamilyId, query.UserId);
    var tasks = await taskAppRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskDto>>.Success(tasks);
  }
}

public class GetOtherTasksDtoBySpec : Specification<TaskInstance, TaskDto>
{
  public GetOtherTasksDtoBySpec(Guid familyId, Guid userId)
  {
    Query
      .Where(t => t.FamilyId == familyId &&
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress) &&
                  t.AssignedToMember != null &&
                  t.AssignedToMember.UserId != userId)
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
