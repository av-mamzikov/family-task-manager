using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplatesByPetQuery(Guid PetId, Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesByPetHandler(IRepository<TaskTemplate> templateRepository)
  : IQueryHandler<GetTaskTemplatesByPetQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesByPetQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoByPetIdsSpec(new[] { request.PetId });
    var templates = await templateRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskTemplateDto>>.Success(templates);
  }
}
