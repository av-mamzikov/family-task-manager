using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

/// <summary>
///   Command to create a one-time task.
///   Note: DueAt parameter should be provided in the family's local timezone.
///   It will be converted to UTC for storage in the database.
/// </summary>
public record CreateTaskCommand(
  Guid FamilyId,
  Guid SpotId,
  string Title,
  TaskPoints Points,
  DateTime DueAt,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Spot> spotAppRepository,
  ITimeZoneService timeZoneService,
  ISpotMoodCalculator moodCalculator) : ICommandHandler<CreateTaskCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
  {
    // Load Spot with family (needed for TaskCreatedEvent)
    var spotSpec = new GetSpotByIdWithFamilySpec(command.SpotId);
    var spot = await spotAppRepository.FirstOrDefaultAsync(spotSpec, cancellationToken);
    if (spot == null) return Result<Guid>.NotFound("Спот не найден");

    if (spot.FamilyId != command.FamilyId) return Result<Guid>.Error("Спот не принадлежит этой семье");

    // Validate title length
    if (command.Title.Length < 3 || command.Title.Length > 100)
      return Result<Guid>.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));

    // Convert DueAt from family timezone to UTC for storage
    DateTime dueAtUtc;
    try
    {
      dueAtUtc = timeZoneService.ConvertToUtc(command.DueAt, spot.Family.Timezone);
    }
    catch (ArgumentException ex)
    {
      return Result<Guid>.Invalid(new ValidationError($"Ошибка преобразования часового пояса: {ex.Message}"));
    }

    // Create one-time task (Spot has Family loaded for event)
    var task = new TaskInstance(
      spot,
      command.Title,
      command.Points,
      dueAtUtc);

    await taskAppRepository.AddAsync(task, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the Spot
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(command.SpotId, cancellationToken);
    spot.UpdateMoodScore(newMoodScore);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result<Guid>.Success(task.Id);
  }
}
