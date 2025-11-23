namespace FamilyTaskManager.Core.Interfaces;

/// <summary>
///   Domain service for evaluating task schedules and determining when instances should be created.
///   Abstracts away the technical details of schedule parsing (Cron, etc.).
/// </summary>
public interface IScheduleEvaluator
{
  /// <summary>
  ///   Gets the next occurrence of a schedule after the specified time.
  /// </summary>
  /// <param name="scheduleExpression">The schedule expression (e.g., Cron expression)</param>
  /// <param name="after">The time to search from</param>
  /// <param name="timezoneId">Optional timezone identifier for schedule evaluation</param>
  /// <returns>The next occurrence time, or null if no valid schedule</returns>
  DateTime? GetNextOccurrence(string scheduleExpression, DateTime after, string? timezoneId = null);

  /// <summary>
  ///   Determines if a schedule should trigger within the specified time window.
  /// </summary>
  /// <param name="scheduleExpression">The schedule expression</param>
  /// <param name="windowStart">Start of the time window</param>
  /// <param name="windowEnd">End of the time window</param>
  /// <param name="timezoneId">Timezone identifier for schedule evaluation</param>
  /// <returns>True if the schedule triggers within the window, along with the trigger time</returns>
  (bool shouldTrigger, DateTime? triggerTime) ShouldTriggerInWindow(string scheduleExpression, DateTime windowStart,
    DateTime windowEnd, string timezoneId);
}
