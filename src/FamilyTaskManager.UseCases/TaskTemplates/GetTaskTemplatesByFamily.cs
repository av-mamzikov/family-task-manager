using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record TaskTemplateDto(
  Guid Id,
  string Title,
  int Points,
  string Schedule,
  Guid PetId,
  string PetName,
  bool IsActive,
  DateTime CreatedAt);

public record GetTaskTemplatesByFamilyQuery(Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesByFamilyHandler(
  IRepository<TaskTemplate> templateRepository,
  IRepository<Pet> petRepository)
  : IQueryHandler<GetTaskTemplatesByFamilyQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesByFamilyQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesByFamilySpec(request.FamilyId, request.IsActive);
    var templates = await templateRepository.ListAsync(spec, cancellationToken);

    if (templates.Count == 0)
    {
      return Result<List<TaskTemplateDto>>.Success(new List<TaskTemplateDto>());
    }

    // Get pet names for all templates
    var petIds = templates.Select(t => t.PetId).Distinct().ToList();
    var pets = await petRepository.ListAsync(new PetsByIdsSpec(petIds), cancellationToken);
    var petDict = pets.ToDictionary(p => p.Id, p => p.Name);

    var result = templates.Select(t => new TaskTemplateDto(
      t.Id,
      t.Title,
      t.Points,
      t.Schedule,
      t.PetId,
      petDict.GetValueOrDefault(t.PetId, "Unknown Pet"),
      t.IsActive,
      t.CreatedAt)).ToList();

    return Result<List<TaskTemplateDto>>.Success(result);
  }
}
