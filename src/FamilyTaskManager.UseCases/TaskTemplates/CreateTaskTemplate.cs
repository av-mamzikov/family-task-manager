namespace FamilyTaskManager.UseCases.TaskTemplates;

public record CreateTaskTemplateCommand(
  Guid FamilyId,
  Guid PetId,
  string Title,
  int Points,
  string Schedule,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskTemplateHandler(
  IRepository<TaskTemplate> templateRepository,
  IRepository<Pet> petRepository) : ICommandHandler<CreateTaskTemplateCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    // Verify pet exists and belongs to family
    var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);
    if (pet == null)
    {
      return Result<Guid>.NotFound("Pet not found");
    }

    if (pet.FamilyId != command.FamilyId)
    {
      return Result<Guid>.Error("Pet does not belong to this family");
    }

    // Validate title
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Title must be between 3 and 100 characters"));
    }

    // Validate points
    if (command.Points < 1 || command.Points > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Points must be between 1 and 100"));
    }

    // Validate schedule (basic check - Quartz will validate the cron expression)
    if (string.IsNullOrWhiteSpace(command.Schedule))
    {
      return Result<Guid>.Invalid(new ValidationError("Schedule is required"));
    }

    // Create template
    var template = new TaskTemplate(
      command.FamilyId,
      command.PetId,
      command.Title,
      command.Points,
      command.Schedule,
      command.CreatedBy);

    await templateRepository.AddAsync(template, cancellationToken);

    return Result<Guid>.Success(template.Id);
  }
}
