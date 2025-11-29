namespace FamilyTaskManager.UseCases.TaskTemplates;

public record CreateTaskTemplateCommand(
  Guid FamilyId,
  Guid PetId,
  string Title,
  TaskPoints Points,
  ScheduleType ScheduleType,
  TimeOnly ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  TimeSpan DueDuration,
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

    // Create schedule
    var scheduleResult = command.ScheduleType.Name switch
    {
      nameof(ScheduleType.Daily) => Schedule.CreateDaily(command.ScheduleTime),
      nameof(ScheduleType.Weekly) => command.ScheduleDayOfWeek.HasValue
        ? Schedule.CreateWeekly(command.ScheduleTime, command.ScheduleDayOfWeek.Value)
        : Result<Schedule>.Error("DayOfWeek is required for Weekly schedule"),
      nameof(ScheduleType.Monthly) => command.ScheduleDayOfMonth.HasValue
        ? Schedule.CreateMonthly(command.ScheduleTime, command.ScheduleDayOfMonth.Value)
        : Result<Schedule>.Error("DayOfMonth is required for Monthly schedule"),
      nameof(ScheduleType.Workdays) => Schedule.CreateWorkdays(command.ScheduleTime),
      nameof(ScheduleType.Weekends) => Schedule.CreateWeekends(command.ScheduleTime),
      nameof(ScheduleType.Manual) => Schedule.CreateManual(),
      _ => Result<Schedule>.Error("Invalid schedule type")
    };

    if (!scheduleResult.IsSuccess)
    {
      return Result<Guid>.Invalid(new ValidationError(scheduleResult.Errors.First()));
    }

    // Create template
    var template = new TaskTemplate(
      command.FamilyId,
      command.PetId,
      command.Title,
      command.Points,
      scheduleResult.Value,
      command.DueDuration,
      command.CreatedBy);

    await templateRepository.AddAsync(template, cancellationToken);

    return Result<Guid>.Success(template.Id);
  }
}
