namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;

public record UpdateTaskTemplateCommand(
  Guid TemplateId,
  Guid FamilyId,
  string? Title,
  TaskPoints? Points,
  ScheduleType? ScheduleType,
  TimeOnly? ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  TimeSpan? DueDuration) : ICommand<Result>;

public class UpdateTaskTemplateHandler(IAppRepository<TaskTemplate> templateAppRepository)
  : ICommandHandler<UpdateTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    var template = await templateAppRepository.GetByIdAsync(command.TemplateId, cancellationToken);
    if (template == null) return Result.NotFound("Шаблон задачи не найден");

    // Authorization check - ensure template belongs to the requested family
    if (template.FamilyId != command.FamilyId) return Result.NotFound("Шаблон задачи не найден");

    // Get current values or use new ones
    var newTitle = command.Title != null ? new(command.Title) : template.Title;
    var newPoints = command.Points ?? template.Points;
    var newSchedule = template.Schedule;

    // If schedule parameters are provided, create new schedule
    if (command.ScheduleType != null && command.ScheduleTime != null)
    {
      var scheduleResult = command.ScheduleType.Name switch
      {
        nameof(ScheduleType.Daily) => Schedule.CreateDaily(command.ScheduleTime.Value),
        nameof(ScheduleType.Weekly) => command.ScheduleDayOfWeek.HasValue
          ? Schedule.CreateWeekly(command.ScheduleTime.Value, command.ScheduleDayOfWeek.Value)
          : Result<Schedule>.Error("DayOfWeek is required for Weekly schedule"),
        nameof(ScheduleType.Monthly) => command.ScheduleDayOfMonth.HasValue
          ? Schedule.CreateMonthly(command.ScheduleTime.Value, command.ScheduleDayOfMonth.Value)
          : Result<Schedule>.Error("DayOfMonth is required for Monthly schedule"),
        nameof(ScheduleType.Workdays) => Schedule.CreateWorkdays(command.ScheduleTime.Value),
        nameof(ScheduleType.Weekends) => Schedule.CreateWeekends(command.ScheduleTime.Value),
        nameof(ScheduleType.Manual) => Schedule.CreateManual(),
        _ => Result<Schedule>.Error("Invalid schedule type")
      };

      if (!scheduleResult.IsSuccess) return Result.Invalid(new ValidationError(scheduleResult.Errors.First()));

      newSchedule = scheduleResult.Value;
    }

    // Update template
    var newDueDuration = command.DueDuration.HasValue
      ? new(command.DueDuration.Value)
      : template.DueDuration;

    template.Update(newTitle, newPoints, newSchedule, newDueDuration);
    await templateAppRepository.UpdateAsync(template, cancellationToken);

    return Result.Success();
  }
}
