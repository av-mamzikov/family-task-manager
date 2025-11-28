using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
///   Command to create a one-time task.
///   Note: DueAt parameter should be provided in the family's local timezone.
///   It will be converted to UTC for storage in the database.
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
  ITimeZoneService timeZoneService,
  IPetMoodCalculator moodCalculator) : ICommandHandler<CreateTaskCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
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

    // Get family for timezone conversion
    var family = await familyRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null)
    {
      return Result<Guid>.NotFound("Семья не найдена");
    }

    // Validate title length
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));
    }

    // Validate points range
    if (command.Points < 1 || command.Points > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Очки должны быть в диапазоне от 1 до 100"));
    }

    // Convert DueAt from family timezone to UTC for storage
    DateTime dueAtUtc;
    try
    {
      dueAtUtc = timeZoneService.ConvertToUtc(command.DueAt, family.Timezone);
    }
    catch (ArgumentException ex)
    {
      return Result<Guid>.Invalid(new ValidationError($"Ошибка преобразования часового пояса: {ex.Message}"));
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
    await taskRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the pet
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(command.PetId, cancellationToken);
    pet.UpdateMoodScore(newMoodScore);
    await petRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(task.Id);
  }
}
