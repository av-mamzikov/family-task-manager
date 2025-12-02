namespace FamilyTaskManager.UseCases.Pets;

public record CreatePetCommand(Guid FamilyId, PetType Type, string Name) : ICommand<Result<Guid>>;

public class CreatePetHandler(
  IAppRepository<Pet> petAppRepository,
  IAppRepository<Family> familyAppRepository,
  IAppRepository<TaskTemplate> taskTemplateAppRepository) : ICommandHandler<CreatePetCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreatePetCommand command, CancellationToken cancellationToken)
  {
    // Verify family exists
    var family = await familyAppRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null)
    {
      return Result<Guid>.NotFound("Семья не найдена");
    }

    // Validate name
    if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length < 2 || command.Name.Length > 50)
    {
      return Result<Guid>.Invalid(new ValidationError("Имя питомца должно быть длиной от 2 до 50 символов"));
    }

    // Create pet
    var pet = new Pet(command.FamilyId, command.Type, command.Name);
    await petAppRepository.AddAsync(pet, cancellationToken);
    await petAppRepository.SaveChangesAsync(cancellationToken);

    // Create default task templates for this pet type
    var defaultTemplates = PetTaskTemplateData.GetDefaultTemplates(command.Type);
    // Use a well-known GUID for system-created templates (not Guid.Empty to pass validation)
    var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");

    foreach (var templateData in defaultTemplates)
    {
      var taskTemplate = new TaskTemplate(
        command.FamilyId,
        pet.Id,
        templateData.Title,
        templateData.GetTaskPoints(),
        templateData.Schedule,
        templateData.DueDuration,
        systemUserId);

      await taskTemplateAppRepository.AddAsync(taskTemplate, cancellationToken);
    }

    await taskTemplateAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(pet.Id);
  }
}
