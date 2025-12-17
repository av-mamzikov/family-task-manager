using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for formatting Schedule Value Object to human-readable text.
/// </summary>
public static class ScheduleFormatter
{
  public static string Format(Schedule schedule) =>
    schedule.Type.Name switch
    {
      nameof(ScheduleType.Daily) => $"Ежедневно в {schedule.Time:HH:mm}",
      nameof(ScheduleType.Weekly) =>
        $"Еженедельно по {GetDayOfWeekName(schedule.DayOfWeek!.Value)} в {schedule.Time:HH:mm}",
      nameof(ScheduleType.Monthly) => $"Ежемесячно {schedule.DayOfMonth}-го числа в {schedule.Time:HH:mm}",
      nameof(ScheduleType.Workdays) => $"По будням в {schedule.Time:HH:mm}",
      nameof(ScheduleType.Weekends) => $"По выходным в {schedule.Time:HH:mm}",
      nameof(ScheduleType.Manual) => "Вручную",
      _ => "Неизвестное расписание"
    };

  public static string Format(ScheduleType type, TimeOnly time, DayOfWeek? dayOfWeek, int? dayOfMonth) =>
    type.Name switch
    {
      nameof(ScheduleType.Daily) => $"Ежедневно в {time:HH:mm}",
      nameof(ScheduleType.Weekly) => dayOfWeek.HasValue
        ? $"Еженедельно по {GetDayOfWeekName(dayOfWeek.Value)} в {time:HH:mm}"
        : "Еженедельно (день не указан)",
      nameof(ScheduleType.Monthly) => dayOfMonth.HasValue
        ? $"Ежемесячно {dayOfMonth}-го числа в {time:HH:mm}"
        : "Ежемесячно (день не указан)",
      nameof(ScheduleType.Workdays) => $"По будням в {time:HH:mm}",
      nameof(ScheduleType.Weekends) => $"По выходным в {time:HH:mm}",
      nameof(ScheduleType.Manual) => "Вручную",
      _ => "Неизвестное расписание"
    };

  private static string GetDayOfWeekName(DayOfWeek dayOfWeek) =>
    dayOfWeek switch
    {
      DayOfWeek.Monday => "понедельникам",
      DayOfWeek.Tuesday => "вторникам",
      DayOfWeek.Wednesday => "средам",
      DayOfWeek.Thursday => "четвергам",
      DayOfWeek.Friday => "пятницам",
      DayOfWeek.Saturday => "субботам",
      DayOfWeek.Sunday => "воскресеньям",
      _ => "неизвестным дням"
    };
}
