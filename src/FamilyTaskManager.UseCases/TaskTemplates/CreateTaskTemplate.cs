namespace FamilyTaskManager.UseCases.TaskTemplates;

public record CreateTaskTemplateCommand(
  Guid FamilyId,
  Guid SpotId,
  string Title,
  TaskPoints Points,
  ScheduleType ScheduleType,
  TimeOnly ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  TimeSpan DueDuration,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskTemplateHandler(
  IAppRepository<TaskTemplate> templateAppRepository,
  IAppRepository<Spot> SpotAppRepository) : ICommandHandler<CreateTaskTemplateCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    // Verify Spot exists and belongs to family
    var Spot = await SpotAppRepository.GetByIdAsync(command.SpotId, cancellationToken);
    if (Spot == null) return Result<Guid>.NotFound("Спот не найден");

    if (Spot.FamilyId != command.FamilyId) return Result<Guid>.Error("Спот не принадлежит этой семье");

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

    if (!scheduleResult.IsSuccess) return Result<Guid>.Invalid(new ValidationError(scheduleResult.Errors.First()));

    // Create template
    var title = new TaskTitle(command.Title);
    var dueDuration = new DueDuration(command.DueDuration);

    var template = new TaskTemplate(
      command.FamilyId,
      command.SpotId,
      title,
      command.Points,
      scheduleResult.Value,
      dueDuration,
      command.CreatedBy);

    await templateAppRepository.AddAsync(template, cancellationToken);

    return Result<Guid>.Success(template.Id);
  }
}
