namespace FamilyTaskManager.UseCases.Spots;

public record CreateSpotCommand(Guid FamilyId, SpotType Type, string Name) : ICommand<Result<Guid>>;

public class CreateSpotHandler(
  IAppRepository<Spot> spotAppRepository,
  IAppRepository<Family> familyAppRepository,
  IAppRepository<TaskTemplate> taskTemplateAppRepository) : ICommandHandler<CreateSpotCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateSpotCommand command, CancellationToken cancellationToken)
  {
    // Verify family exists
    var family = await familyAppRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null) return Result<Guid>.NotFound("Семья не найдена");

    // Validate name
    if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length < 2 || command.Name.Length > 50)
      return Result<Guid>.Invalid(new ValidationError("Имя спота должно быть длиной от 2 до 50 символов"));

    // Create Spot
    var spot = new Spot(command.FamilyId, command.Type, command.Name);
    await spotAppRepository.AddAsync(spot, cancellationToken);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    // Create default task templates for this spot type
    var defaultTemplates = SpotTaskTemplateData.GetDefaultTemplates(command.Type);
    // Use a well-known GUID for system-created templates (not Guid.Empty to pass validation)
    var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");

    foreach (var templateData in defaultTemplates)
    {
      var taskTemplate = new TaskTemplate(
        command.FamilyId,
        spot.Id,
        new(templateData.Title),
        templateData.GetTaskPoints(),
        templateData.Schedule,
        new(templateData.DueDuration),
        systemUserId);

      await taskTemplateAppRepository.AddAsync(taskTemplate, cancellationToken);
    }

    await taskTemplateAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(spot.Id);
  }
}
