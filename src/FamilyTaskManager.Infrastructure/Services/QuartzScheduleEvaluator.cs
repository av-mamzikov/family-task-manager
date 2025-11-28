using FamilyTaskManager.Core.Interfaces;
using Quartz;

namespace FamilyTaskManager.Infrastructure.Services;

/// <summary>
///   Infrastructure implementation of IScheduleEvaluator using Quartz CronExpression.
///   Handles the technical details of Cron parsing and evaluation.
/// </summary>
public class QuartzScheduleEvaluator : IScheduleEvaluator
{
  public DateTime? GetNextOccurrence(string scheduleExpression, DateTime after, string? timezoneId = null)
  {
    try
    {
      var cronExpression = new CronExpression(scheduleExpression);

      // Set timezone if provided
      if (!string.IsNullOrWhiteSpace(timezoneId))
      {
        cronExpression.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
      }

      var nextOccurrence = cronExpression.GetTimeAfter(new DateTimeOffset(after));
      return nextOccurrence?.UtcDateTime;
    }
    catch (Exception)
    {
      // Invalid cron expression or other parsing error
      return null;
    }
  }

  public DateTime? ShouldTriggerInWindow(string scheduleExpression,
    DateTime windowStart, DateTime windowEnd, string? timezoneId = null)
  {
    try
    {
      var cronExpression = new CronExpression(scheduleExpression);

      // Set timezone if provided
      TimeZoneInfo? timeZone = null;
      if (!string.IsNullOrWhiteSpace(timezoneId))
      {
        timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        cronExpression.TimeZone = timeZone;
      }

      // Convert windowStart to the target timezone for cron evaluation
      var windowStartInTz = timeZone != null
        ? TimeZoneInfo.ConvertTime(windowStart, timeZone)
        : new DateTimeOffset(windowStart);

      // Get the next occurrence after the window start (in target timezone)
      var nextOccurrence = cronExpression.GetTimeAfter(windowStartInTz);

      if (!nextOccurrence.HasValue)
        return null;

      var occurrenceTime = nextOccurrence.Value.UtcDateTime;

      // Check if the occurrence falls within our window (both in UTC)
      if (occurrenceTime > windowStart && occurrenceTime <= windowEnd)
        return occurrenceTime;

      return null;
    }
    catch (Exception)
    {
      return null;
    }
  }
}
