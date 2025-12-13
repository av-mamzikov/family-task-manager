using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;

public record GetTaskTemplateByIdQuery(Guid Id, Guid FamilyId)
  : IQuery<Result<TaskTemplateDto>>;

public class GetTaskTemplateByIdHandler(IAppReadRepository<TaskTemplate> templateRepository)
  : IQueryHandler<GetTaskTemplateByIdQuery, Result<TaskTemplateDto>>
{
  public async ValueTask<Result<TaskTemplateDto>> Handle(GetTaskTemplateByIdQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoByIdsSpec([request.Id]);
    var template = await templateRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (template == null || template.FamilyId != request.FamilyId)
      return Result<TaskTemplateDto>.NotFound("Task template not found");

    return Result<TaskTemplateDto>.Success(template);
  }
}

public class TaskTemplatesDtoByIdsSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public TaskTemplatesDtoByIdsSpec(IEnumerable<Guid> templateIds)
  {
    Query
      .Where(t => templateIds.Contains(t.Id))
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
