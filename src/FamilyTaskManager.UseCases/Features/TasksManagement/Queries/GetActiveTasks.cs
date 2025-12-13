using FamilyTaskManager.Core.TaskAggregate.DTOs;
using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Queries;

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

public class ActiveTaskTemplatesDtoSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public ActiveTaskTemplatesDtoSpec()
  {
    Query.Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}

public class GetActiveTasksDtoByFamilySpec : Specification<TaskInstance, TaskDto>
{
  public GetActiveTasksDtoByFamilySpec(Guid familyId)
  {
    Query
      .Where(t => t.FamilyId == familyId &&
                  (t.Status == TaskStatus.Active || t.Status == TaskStatus.InProgress))
      .OrderBy(t => t.DueAt)
      .Select(TaskDto.Projections.FromTaskInstance);
  }
}
