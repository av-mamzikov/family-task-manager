using System.Linq.Expressions;

namespace FamilyTaskManager.UseCases.Contracts.TaskTemplates;

public record TaskTemplateDto(
  Guid Id,
  Guid FamilyId,
  string Title,
  TaskPoints Points,
  ScheduleType ScheduleType,
  TimeOnly ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  Guid SpotId,
  string SpotName,
  DateTime CreatedAt,
  TimeSpan DueDuration)
{
  public static class Projections
  {
    public static readonly Expression<Func<TaskTemplate, TaskTemplateDto>> FromTaskTemplate =
      t => new TaskTemplateDto(
        t.Id,
        t.FamilyId,
        t.Title,
        t.Points,
        t.Schedule.Type,
        t.Schedule.Time,
        t.Schedule.DayOfWeek,
        t.Schedule.DayOfMonth,
        t.SpotId,
        t.Spot.Name,
        t.CreatedAt,
        t.DueDuration);
  }
}
