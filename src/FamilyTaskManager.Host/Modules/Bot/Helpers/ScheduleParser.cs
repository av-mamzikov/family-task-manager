using Ardalis.Result;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for parsing user-friendly schedule text into Schedule parameters.
/// </summary>
public static class ScheduleParser
{
  public static Result<(ScheduleType Type, TimeOnly Time, DayOfWeek? DayOfWeek, int? DayOfMonth)> Parse(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return Result.Error("Расписание не может быть пустым");

    var parts = input.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length < 2)
      return Result.Error(
        "Неверный формат. Используйте: 'ежедневно 10:00' или 'еженедельно пн 15:00' или 'ежемесячно 1 12:00'");

    // Parse schedule type
    var scheduleType = parts[0] switch
    {
      "ежедневно" or "каждый день" or "daily" => ScheduleType.Daily,
      "еженедельно" or "каждую неделю" or "weekly" => ScheduleType.Weekly,
      "ежемесячно" or "каждый месяц" or "monthly" => ScheduleType.Monthly,
      "будни" or "workdays" => ScheduleType.Workdays,
      "выходные" or "weekends" => ScheduleType.Weekends,
      _ => null
    };

    if (scheduleType == null)
      return Result.Error(
        "Неизвестный тип расписания. Используйте: ежедневно, еженедельно, ежемесячно, будни, выходные");

    // For Daily, Workdays, Weekends: format is "type time"
    if (scheduleType == ScheduleType.Daily || scheduleType == ScheduleType.Workdays ||
        scheduleType == ScheduleType.Weekends)
    {
      if (parts.Length != 2)
        return Result.Error($"Для типа '{parts[0]}' укажите только время. Например: '{parts[0]} 10:00'");

      if (!TimeOnly.TryParse(parts[1], out var time))
        return Result.Error($"Неверный формат времени '{parts[1]}'. Используйте формат HH:mm, например: 10:00");

      return Result.Success((scheduleType, time, (DayOfWeek?)null, (int?)null));
    }

    // For Weekly: format is "еженедельно день time"
    if (scheduleType == ScheduleType.Weekly)
    {
      if (parts.Length != 3)
        return Result.Error(
          "Для еженедельного расписания укажите день недели и время. Например: 'еженедельно пн 15:00'");

      var dayOfWeek = ParseDayOfWeek(parts[1]);
      if (!dayOfWeek.HasValue)
        return Result.Error($"Неверный день недели '{parts[1]}'. Используйте: пн, вт, ср, чт, пт, сб, вс");

      if (!TimeOnly.TryParse(parts[2], out var time))
        return Result.Error($"Неверный формат времени '{parts[2]}'. Используйте формат HH:mm, например: 15:00");

      return Result.Success((scheduleType, time, dayOfWeek, (int?)null));
    }

    // For Monthly: format is "ежемесячно день time"
    if (scheduleType == ScheduleType.Monthly)
    {
      if (parts.Length != 3)
        return Result.Error("Для ежемесячного расписания укажите день месяца и время. Например: 'ежемесячно 1 12:00'");

      if (!int.TryParse(parts[1], out var dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
        return Result.Error($"Неверный день месяца '{parts[1]}'. Укажите число от 1 до 31");

      if (!TimeOnly.TryParse(parts[2], out var time))
        return Result.Error($"Неверный формат времени '{parts[2]}'. Используйте формат HH:mm, например: 12:00");

      return Result.Success((scheduleType, time, (DayOfWeek?)null, (int?)dayOfMonth));
    }

    return Result.Error("Неизвестная ошибка парсинга расписания");
  }

  private static DayOfWeek? ParseDayOfWeek(string input) =>
    input.ToLowerInvariant() switch
    {
      "пн" or "понедельник" or "monday" or "mon" => DayOfWeek.Monday,
      "вт" or "вторник" or "tuesday" or "tue" => DayOfWeek.Tuesday,
      "ср" or "среда" or "wednesday" or "wed" => DayOfWeek.Wednesday,
      "чт" or "четверг" or "thursday" or "thu" => DayOfWeek.Thursday,
      "пт" or "пятница" or "friday" or "fri" => DayOfWeek.Friday,
      "сб" or "суббота" or "saturday" or "sat" => DayOfWeek.Saturday,
      "вс" or "воскресенье" or "sunday" or "sun" => DayOfWeek.Sunday,
      _ => null
    };
}
