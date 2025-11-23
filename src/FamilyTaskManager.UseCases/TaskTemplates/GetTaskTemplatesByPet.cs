using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplatesByPetQuery(Guid PetId, Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesByPetHandler(IRepository<TaskTemplate> templateRepository, IRepository<Pet> petRepository)
  : IQueryHandler<GetTaskTemplatesByPetQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesByPetQuery request,
    CancellationToken cancellationToken)
  {
    // Verify pet exists and belongs to family
    var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
    if (pet == null)
    {
      return Result<List<TaskTemplateDto>>.NotFound("Pet not found");
    }

    if (pet.FamilyId != request.FamilyId)
    {
      return Result<List<TaskTemplateDto>>.NotFound("Pet not found");
    }

    var spec = new TaskTemplatesByPetSpec(request.PetId, request.IsActive);
    var templates = await templateRepository.ListAsync(spec, cancellationToken);

    if (templates.Count == 0)
    {
      return Result<List<TaskTemplateDto>>.Success(new List<TaskTemplateDto>());
    }

    var result = templates.Select(t => new TaskTemplateDto(
      t.Id,
      t.Title,
      t.Points,
      t.Schedule,
      t.PetId,
      pet.Name,
      t.IsActive,
      t.CreatedAt)).ToList();

    return Result<List<TaskTemplateDto>>.Success(result);
  }
}
