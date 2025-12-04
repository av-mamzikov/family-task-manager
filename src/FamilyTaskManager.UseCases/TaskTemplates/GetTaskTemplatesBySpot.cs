using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplatesBySpotQuery(Guid SpotId, Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesBySpotHandler(IAppRepository<TaskTemplate> templateAppRepository)
  : IQueryHandler<GetTaskTemplatesBySpotQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesBySpotQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoBySpotIdsSpec(new[] { request.SpotId });
    var templates = await templateAppRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskTemplateDto>>.Success(templates);
  }
}
