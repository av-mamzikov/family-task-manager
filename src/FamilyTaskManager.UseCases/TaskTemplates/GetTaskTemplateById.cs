using FamilyTaskManager.UseCases.Contracts.TaskTemplates;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplateByIdQuery(Guid Id, Guid FamilyId)
  : IQuery<Result<TaskTemplateDto>>;

public class GetTaskTemplateByIdHandler(IReadRepository<TaskTemplate> templateRepository)
  : IQueryHandler<GetTaskTemplateByIdQuery, Result<TaskTemplateDto>>
{
  public async ValueTask<Result<TaskTemplateDto>> Handle(GetTaskTemplateByIdQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoByIdsSpec(new[] { request.Id });
    var template = await templateRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (template == null || template.FamilyId != request.FamilyId)
      return Result<TaskTemplateDto>.NotFound("Task template not found");

    return Result<TaskTemplateDto>.Success(template);
  }
}
