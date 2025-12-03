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
  TaskPoints Points,
  DateTime DueAt,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Pet> petAppRepository,
  ITimeZoneService timeZoneService,
  IPetMoodCalculator moodCalculator) : ICommandHandler<CreateTaskCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
  {
    // Load pet with family (needed for TaskCreatedEvent)
    var petSpec = new GetPetByIdWithFamilySpec(command.PetId);
    var pet = await petAppRepository.FirstOrDefaultAsync(petSpec, cancellationToken);
    if (pet == null)
    {
      return Result<Guid>.NotFound("Питомец не найден");
    }

    if (pet.FamilyId != command.FamilyId)
    {
      return Result<Guid>.Error("Питомец не принадлежит этой семье");
    }

    // Validate title length
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));
    }

    // Convert DueAt from family timezone to UTC for storage
    DateTime dueAtUtc;
    try
    {
      dueAtUtc = timeZoneService.ConvertToUtc(command.DueAt, pet.Family.Timezone);
    }
    catch (ArgumentException ex)
    {
      return Result<Guid>.Invalid(new ValidationError($"Ошибка преобразования часового пояса: {ex.Message}"));
    }

    // Create one-time task (pet has Family loaded for event)
    var task = new TaskInstance(
      pet,
      command.Title,
      command.Points,
      TaskType.OneTime,
      dueAtUtc);

    await taskAppRepository.AddAsync(task, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the pet
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(command.PetId, cancellationToken);
    pet.UpdateMoodScore(newMoodScore);
    await petAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(task.Id);
  }
}
