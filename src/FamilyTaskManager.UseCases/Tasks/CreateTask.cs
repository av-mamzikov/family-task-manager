using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
/// Command to create a one-time task.
/// Note: DueAt parameter should be provided in the family's local timezone.
/// It will be converted to UTC for storage in the database.
/// </summary>
public record CreateTaskCommand(
  Guid FamilyId, 
  Guid PetId, 
  string Title, 
  int Points, 
  DateTime DueAt,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Pet> petRepository,
  IRepository<Family> familyRepository,
  ITimeZoneService timeZoneService) : ICommandHandler<CreateTaskCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
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

    // Get family for timezone conversion
    var family = await familyRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null)
    {
      return Result<Guid>.NotFound("Family not found");
    }

    // Validate title length
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Title must be between 3 and 100 characters"));
    }

    // Validate points range
    if (command.Points < 1 || command.Points > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Points must be between 1 and 100"));
    }

    // Convert DueAt from family timezone to UTC for storage
    DateTime dueAtUtc;
    try
    {
      dueAtUtc = timeZoneService.ConvertToUtc(command.DueAt, family.Timezone);
    }
    catch (ArgumentException ex)
    {
      return Result<Guid>.Invalid(new ValidationError($"Invalid timezone conversion: {ex.Message}"));
    }

    // Create one-time task
    var task = new TaskInstance(
      command.FamilyId,
      command.PetId,
      command.Title,
      command.Points,
      TaskType.OneTime,
      dueAtUtc);

    await taskRepository.AddAsync(task, cancellationToken);

    return Result<Guid>.Success(task.Id);
  }
}
