using System;

namespace FamilyTaskManager.Core.Interfaces;

/// <summary>
/// Service for handling timezone conversions and validation.
/// All task times are stored in UTC but interpreted in family timezone.
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// Validates if the timezone identifier is valid on the system.
    /// </summary>
    bool IsValidTimeZone(string timezoneId);
    
    /// <summary>
    /// Converts a local datetime in the specified timezone to UTC.
    /// </summary>
    DateTime ConvertToUtc(DateTime localDateTime, string timezoneId);
    
    /// <summary>
    /// Converts a UTC datetime to the specified timezone's local time.
    /// </summary>
    DateTime ConvertFromUtc(DateTime utcDateTime, string timezoneId);
    
    /// <summary>
    /// Gets the current time in the specified timezone.
    /// </summary>
    DateTime GetCurrentTimeInTimeZone(string timezoneId);
    
    /// <summary>
    /// Gets the TimeZoneInfo for the specified timezone identifier.
    /// Throws if timezone is invalid.
    /// </summary>
    TimeZoneInfo GetTimeZone(string timezoneId);
}
