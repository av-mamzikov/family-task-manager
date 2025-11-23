namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetTaskTemplateByIdQuery(Guid Id, Guid FamilyId)
  : IQuery<Result<TaskTemplateDto>>;

public class GetTaskTemplateByIdHandler(
  IRepository<TaskTemplate> templateRepository,
  IRepository<Pet> petRepository)
  : IQueryHandler<GetTaskTemplateByIdQuery, Result<TaskTemplateDto>>
{
  public async ValueTask<Result<TaskTemplateDto>> Handle(GetTaskTemplateByIdQuery request,
    CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(request.Id, cancellationToken);

    if (template == null)
    {
      return Result<TaskTemplateDto>.NotFound("Task template not found");
    }

    // Authorization check - ensure template belongs to the requested family
    if (template.FamilyId != request.FamilyId)
    {
      return Result<TaskTemplateDto>.NotFound("Task template not found");
    }

    // Get pet name
    var pet = await petRepository.GetByIdAsync(template.PetId, cancellationToken);
    var petName = pet?.Name ?? "Unknown Pet";

    var result = new TaskTemplateDto(
      template.Id,
      template.Title,
      template.Points,
      template.Schedule,
      template.PetId,
      petName,
      template.IsActive,
      template.CreatedAt);

    return Result<TaskTemplateDto>.Success(result);
  }
}
