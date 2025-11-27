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
      return Result<Guid>.NotFound("Питомец не найден");
    }

    if (pet.FamilyId != command.FamilyId)
    {
      return Result<Guid>.Error("Питомец не принадлежит этой семье");
    }

    // Validate title
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));
    }

    // Validate points
    if (command.Points < 1 || command.Points > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Очки должны быть в диапазоне от 1 до 100"));
    }

    // Validate schedule (basic check - Quartz will validate the cron expression)
    if (string.IsNullOrWhiteSpace(command.Schedule))
    {
      return Result<Guid>.Invalid(new ValidationError("Требуется расписание"));
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
