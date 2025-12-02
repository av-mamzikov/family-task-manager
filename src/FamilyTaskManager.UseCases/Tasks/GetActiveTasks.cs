using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetActiveTasksQuery(Guid FamilyId, Guid UserId) : IQuery<Result<List<TaskDto>>>;

public class GetActiveTasksHandler(IAppRepository<TaskInstance> taskAppRepository)
  : IQueryHandler<GetActiveTasksQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetActiveTasksQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetActiveTasksDtoByFamilySpec(query.FamilyId);
    var tasks = await taskAppRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskDto>>.Success(tasks);
  }
}
