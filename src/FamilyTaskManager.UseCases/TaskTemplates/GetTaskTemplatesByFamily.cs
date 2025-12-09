using FamilyTaskManager.UseCases.Contracts.TaskTemplates;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplatesByFamilyQuery(Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesByFamilyHandler(
  IAppReadRepository<TaskTemplate> templateRepository)
  : IQueryHandler<GetTaskTemplatesByFamilyQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesByFamilyQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoByFamilyIdsSpec(request.FamilyId);
    var templates = await templateRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskTemplateDto>>.Success(templates);
  }
}
