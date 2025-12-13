using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;

public record GetTaskTemplatesBySpotQuery(Guid SpotId, Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesBySpotHandler(IAppRepository<TaskTemplate> templateAppRepository)
  : IQueryHandler<GetTaskTemplatesBySpotQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesBySpotQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoBySpotIdsSpec([request.SpotId]);
    var templates = await templateAppRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskTemplateDto>>.Success(templates);
  }
}

public class TaskTemplatesDtoBySpotIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoBySpotIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query.Where(t => templateIds.Contains(t.SpotId))
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
