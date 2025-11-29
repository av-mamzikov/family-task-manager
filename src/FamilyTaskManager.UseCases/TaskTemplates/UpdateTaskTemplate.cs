namespace FamilyTaskManager.UseCases.TaskTemplates;

public record UpdateTaskTemplateCommand(
  Guid TemplateId,
  Guid FamilyId,
  string? Title,
  int? Points,
  ScheduleType? ScheduleType,
  TimeOnly? ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  TimeSpan? DueDuration) : ICommand<Result>;

public class UpdateTaskTemplateHandler(IRepository<TaskTemplate> templateRepository)
  : ICommandHandler<UpdateTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
    if (template == null)
    {
      return Result.NotFound("Шаблон задачи не найден");
    }

    // Authorization check - ensure template belongs to the requested family
    if (template.FamilyId != command.FamilyId)
    {
      return Result.NotFound("Шаблон задачи не найден");
    }

    if (!template.IsActive)
    {
      return Result.Error("Нельзя изменить неактивный шаблон");
    }

    // Get current values or use new ones
    var newTitle = command.Title ?? template.Title;
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

    // Validate title
    if (newTitle.Length < 3 || newTitle.Length > 100)
    {
      return Result.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));
    }

    // Validate points
    if (newPoints < 1 || newPoints > 100)
    {
      return Result.Invalid(new ValidationError("Очки должны быть в диапазоне от 1 до 100"));
    }

    // Update template
    var newDueDuration = command.DueDuration ?? template.DueDuration;

    // Validate dueDuration
    if (newDueDuration < TimeSpan.Zero || newDueDuration > TimeSpan.FromHours(24))
      return Result.Invalid(new ValidationError("Срок выполнения должен быть в диапазоне от 0 до 24 часов"));

    template.Update(newTitle, newPoints, newSchedule, newDueDuration);
    await templateRepository.UpdateAsync(template, cancellationToken);

    return Result.Success();
  }
}
