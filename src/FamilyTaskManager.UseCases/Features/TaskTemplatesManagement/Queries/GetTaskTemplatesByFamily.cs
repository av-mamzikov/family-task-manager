using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;

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

public class TaskTemplatesDtoByFamilyIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoByFamilyIdsSpec(Guid familyId)
  {
    Query.Where(t => t.FamilyId == familyId)
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
