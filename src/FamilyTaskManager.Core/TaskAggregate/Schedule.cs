namespace FamilyTaskManager.Core.TaskAggregate;

/// <summary>
///   Value Object representing a task schedule.
///   Encapsulates schedule logic and validation.
/// </summary>
public class Schedule : ValueObject
{
  private Schedule()
  {
    // EF Core
  }

  private Schedule(ScheduleType type, TimeOnly time, DayOfWeek? dayOfWeek, int? dayOfMonth)
  {
    Type = type;
    Time = time;
    DayOfWeek = dayOfWeek;
    DayOfMonth = dayOfMonth;
  }

  public ScheduleType Type { get; } = null!;
  public TimeOnly Time { get; }
  public DayOfWeek? DayOfWeek { get; }
  public int? DayOfMonth { get; }

  /// <summary>
  ///   Creates a daily schedule that triggers every day at the specified time.
  /// </summary>
  public static Result<Schedule> CreateDaily(TimeOnly time) =>
    Result.Success(new Schedule(ScheduleType.Daily, time, null, null));

  /// <summary>
  ///   Creates a weekly schedule that triggers on a specific day of the week at the specified time.
  /// </summary>
  public static Result<Schedule> CreateWeekly(TimeOnly time, DayOfWeek dayOfWeek) =>
    Result.Success(new Schedule(ScheduleType.Weekly, time, dayOfWeek, null));

  /// <summary>
  ///   Creates a monthly schedule that triggers on a specific day of the month at the specified time.
  /// </summary>
  public static Result<Schedule> CreateMonthly(TimeOnly time, int dayOfMonth)
  {
    if (dayOfMonth < 1 || dayOfMonth > 31)
      return Result.Error("Day of month must be between 1 and 31");

    return Result.Success(new Schedule(ScheduleType.Monthly, time, null, dayOfMonth));
  }

  /// <summary>
  ///   Creates a workdays schedule that triggers Monday through Friday at the specified time.
  /// </summary>
  public static Result<Schedule> CreateWorkdays(TimeOnly time) =>
    Result.Success(new Schedule(ScheduleType.Workdays, time, null, null));

  /// <summary>
  ///   Creates a weekends schedule that triggers Saturday and Sunday at the specified time.
  /// </summary>
  public static Result<Schedule> CreateWeekends(TimeOnly time) =>
    Result.Success(new Schedule(ScheduleType.Weekends, time, null, null));

  /// <summary>
  ///   Calculates the next occurrence of this schedule after the specified time.
  /// </summary>
  /// <param name="after">The time after which to find the next occurrence (in UTC)</param>
  /// <param name="timeZone">The timezone to use for schedule evaluation</param>
  /// <returns>The next occurrence time in UTC, or null if there is no next occurrence</returns>
  public DateTime? GetNextOccurrence(DateTime after, TimeZoneInfo timeZone)
  {
    // Convert UTC time to target timezone
    var afterInTz = TimeZoneInfo.ConvertTimeFromUtc(after, timeZone);

    DateTime? nextInTz = Type.Name switch
    {
      nameof(ScheduleType.Daily) => GetNextDaily(afterInTz),
      nameof(ScheduleType.Weekly) => GetNextWeekly(afterInTz),
      nameof(ScheduleType.Monthly) => GetNextMonthly(afterInTz),
      nameof(ScheduleType.Workdays) => GetNextWorkdays(afterInTz),
      nameof(ScheduleType.Weekends) => GetNextWeekends(afterInTz),
      _ => null
    };

    if (!nextInTz.HasValue)
      return null;

    // Convert back to UTC
    return TimeZoneInfo.ConvertTimeToUtc(nextInTz.Value, timeZone);
  }

  /// <summary>
  ///   Checks if this schedule should trigger within the specified time window.
  /// </summary>
  /// <param name="windowStart">Start of the time window (in UTC)</param>
  /// <param name="windowEnd">End of the time window (in UTC)</param>
  /// <param name="timeZone">The timezone to use for schedule evaluation</param>
  /// <returns>The trigger time in UTC if it falls within the window, otherwise null</returns>
  public DateTime? ShouldTriggerInWindow(DateTime windowStart, DateTime windowEnd, TimeZoneInfo timeZone)
  {
    var nextOccurrence = GetNextOccurrence(windowStart, timeZone);

    if (!nextOccurrence.HasValue)
      return null;

    // Check if the occurrence falls within our window (both in UTC)
    if (nextOccurrence.Value > windowStart && nextOccurrence.Value <= windowEnd)
      return nextOccurrence.Value;

    return null;
  }

  private DateTime GetNextDaily(DateTime afterInTz)
  {
    var candidate = afterInTz.Date.Add(Time.ToTimeSpan());

    // If the time today has already passed, move to tomorrow
    if (candidate <= afterInTz)
      candidate = candidate.AddDays(1);

    return candidate;
  }

  private DateTime GetNextWeekly(DateTime afterInTz)
  {
    if (!DayOfWeek.HasValue)
      throw new InvalidOperationException("DayOfWeek must be set for Weekly schedule");

    var candidate = afterInTz.Date.Add(Time.ToTimeSpan());

    // Find the next occurrence of the target day of week
    var daysUntilTarget = ((int)DayOfWeek.Value - (int)candidate.DayOfWeek + 7) % 7;

    if (daysUntilTarget == 0 && candidate <= afterInTz)
      daysUntilTarget = 7; // If today is the target day but time has passed, wait a week

    candidate = candidate.AddDays(daysUntilTarget);
    return candidate;
  }

  private DateTime GetNextMonthly(DateTime afterInTz)
  {
    if (!DayOfMonth.HasValue)
      throw new InvalidOperationException("DayOfMonth must be set for Monthly schedule");

    var candidate = new DateTime(afterInTz.Year, afterInTz.Month, 1).Add(Time.ToTimeSpan());

    // Try to set the day of month, handling months with fewer days
    try
    {
      candidate = new DateTime(candidate.Year, candidate.Month, DayOfMonth.Value,
        Time.Hour, Time.Minute, 0);
    }
    catch (ArgumentOutOfRangeException)
    {
      // If the day doesn't exist in this month (e.g., Feb 31), use the last day of the month
      candidate = new DateTime(candidate.Year, candidate.Month,
        DateTime.DaysInMonth(candidate.Year, candidate.Month),
        Time.Hour, Time.Minute, 0);
    }

    // If this month's occurrence has passed, move to next month
    if (candidate <= afterInTz)
    {
      var nextMonth = candidate.AddMonths(1);
      try
      {
        candidate = new DateTime(nextMonth.Year, nextMonth.Month, DayOfMonth.Value,
          Time.Hour, Time.Minute, 0);
      }
      catch (ArgumentOutOfRangeException)
      {
        candidate = new DateTime(nextMonth.Year, nextMonth.Month,
          DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month),
          Time.Hour, Time.Minute, 0);
      }
    }

    return candidate;
  }

  private DateTime GetNextWorkdays(DateTime afterInTz)
  {
    var candidate = afterInTz.Date.Add(Time.ToTimeSpan());

    // If the time today has already passed, move to next day
    if (candidate <= afterInTz)
      candidate = candidate.AddDays(1);

    // Find the next workday (Monday-Friday)
    while (candidate.DayOfWeek == System.DayOfWeek.Saturday ||
           candidate.DayOfWeek == System.DayOfWeek.Sunday)
      candidate = candidate.AddDays(1);

    return candidate;
  }

  private DateTime GetNextWeekends(DateTime afterInTz)
  {
    var candidate = afterInTz.Date.Add(Time.ToTimeSpan());

    // If the time today has already passed, move to next day
    if (candidate <= afterInTz)
      candidate = candidate.AddDays(1);

    // Find the next weekend day (Saturday or Sunday)
    while (candidate.DayOfWeek != System.DayOfWeek.Saturday &&
           candidate.DayOfWeek != System.DayOfWeek.Sunday)
      candidate = candidate.AddDays(1);

    return candidate;
  }

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Type;
    yield return Time;
    if (DayOfWeek.HasValue) yield return DayOfWeek.Value;
    if (DayOfMonth.HasValue) yield return DayOfMonth.Value;
  }
}
