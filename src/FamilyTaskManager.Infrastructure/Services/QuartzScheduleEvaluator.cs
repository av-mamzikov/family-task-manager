using Quartz;
using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of IScheduleEvaluator using Quartz CronExpression.
/// Handles the technical details of Cron parsing and evaluation.
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

    public (bool shouldTrigger, DateTime? triggerTime) ShouldTriggerInWindow(string scheduleExpression, DateTime windowStart, DateTime windowEnd, string? timezoneId = null)
    {
        try
        {
            var cronExpression = new CronExpression(scheduleExpression);
            
            // Set timezone if provided
            if (!string.IsNullOrWhiteSpace(timezoneId))
            {
                cronExpression.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            }
            
            // Get the next occurrence after the window start
            var nextOccurrence = cronExpression.GetTimeAfter(new DateTimeOffset(windowStart));
            
            if (nextOccurrence.HasValue)
            {
                var occurrenceTime = nextOccurrence.Value.UtcDateTime;
                
                // Check if the occurrence falls within our window
                if (occurrenceTime > windowStart && occurrenceTime <= windowEnd)
                {
                    return (true, occurrenceTime);
                }
            }
            
            return (false, null);
        }
        catch (Exception)
        {
            // Invalid cron expression or other parsing error
            return (false, null);
        }
    }
}
