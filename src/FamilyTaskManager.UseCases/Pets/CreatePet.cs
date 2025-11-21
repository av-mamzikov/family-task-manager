namespace FamilyTaskManager.UseCases.Pets;

public record CreatePetCommand(Guid FamilyId, PetType Type, string Name) : ICommand<Result<Guid>>;

public class CreatePetHandler(
  IRepository<Pet> petRepository,
  IRepository<Family> familyRepository,
  IRepository<TaskTemplate> taskTemplateRepository) : ICommandHandler<CreatePetCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreatePetCommand command, CancellationToken cancellationToken)
  {
    // Verify family exists
    var family = await familyRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null)
    {
      return Result<Guid>.NotFound("Family not found");
    }

    // Validate name
    if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length < 2 || command.Name.Length > 50)
    {
      return Result<Guid>.Invalid(new ValidationError("Pet name must be between 2 and 50 characters"));
    }

    // Create pet
    var pet = new Pet(command.FamilyId, command.Type, command.Name);
    await petRepository.AddAsync(pet, cancellationToken);

    // Create default task templates for this pet type
    var defaultTemplates = PetTaskTemplateData.GetDefaultTemplates(command.Type);
    var systemUserId = Guid.Empty; // System-created templates
    
    foreach (var templateData in defaultTemplates)
    {
      var taskTemplate = new TaskTemplate(
        familyId: command.FamilyId,
        petId: pet.Id,
        title: templateData.Title,
        points: templateData.Points,
        schedule: templateData.Schedule,
        createdBy: systemUserId);
      
      await taskTemplateRepository.AddAsync(taskTemplate, cancellationToken);
    }

    return Result<Guid>.Success(pet.Id);
  }
}
