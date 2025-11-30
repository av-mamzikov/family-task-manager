using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.Infrastructure.Services;

/// <summary>
///   Infrastructure implementation of ITimeZoneService.
///   Handles timezone conversions and validation for family task scheduling.
/// </summary>
public class TimeZoneService : ITimeZoneService
{
  public bool IsValidTimeZone(string timezoneId)
  {
    if (string.IsNullOrWhiteSpace(timezoneId))
    {
      return false;
    }

    try
    {
      TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
      return true;
    }
    catch (TimeZoneNotFoundException)
    {
      return false;
    }
  }

  public DateTime ConvertToUtc(DateTime localDateTime, string timezoneId)
  {
    var timeZone = GetTimeZone(timezoneId);

    if (localDateTime.Kind == DateTimeKind.Utc)
    {
      // Already UTC
      return localDateTime;
    }

    if (localDateTime.Kind == DateTimeKind.Unspecified)
    {
      // Assume the datetime is in the specified timezone
      return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
    }

    // DateTimeKind.Local - convert from local system time to target timezone first, then to UTC
    var targetTime = TimeZoneInfo.ConvertTime(localDateTime, timeZone);
    return TimeZoneInfo.ConvertTimeToUtc(targetTime, timeZone);
  }

  public DateTime ConvertFromUtc(DateTime utcDateTime, string timezoneId)
  {
    if (utcDateTime.Kind != DateTimeKind.Utc)
    {
      throw new ArgumentException("DateTime must be in UTC format", nameof(utcDateTime));
    }

    var timeZone = GetTimeZone(timezoneId);
    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
  }

  public DateTime GetCurrentTimeInTimeZone(string timezoneId)
  {
    var timeZone = GetTimeZone(timezoneId);
    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
  }

  public TimeZoneInfo GetTimeZone(string timezoneId)
  {
    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
    }
    catch (TimeZoneNotFoundException ex)
    {
      throw new ArgumentException($"Invalid timezone identifier: {timezoneId}", nameof(timezoneId), ex);
    }
  }
}
