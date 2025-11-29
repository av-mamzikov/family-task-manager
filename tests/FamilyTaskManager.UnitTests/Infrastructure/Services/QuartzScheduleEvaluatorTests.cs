using FamilyTaskManager.Infrastructure.Services;

namespace FamilyTaskManager.UnitTests.Infrastructure.Services;

/// <summary>
///   Tests for QuartzScheduleEvaluator.
///   Important: Tasks are created in the family's timezone, so all tests use explicit timezones.
/// </summary>
public class QuartzScheduleEvaluatorTests
{
  private readonly QuartzScheduleEvaluator _evaluator = new();

  #region GetNextOccurrence Tests

  [Fact]
  public void GetNextOccurrence_WithValidDailyCron_ReturnsNextOccurrence()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var after = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Hour.ShouldBe(9);
    result.Value.Minute.ShouldBe(0);
    result.Value.Day.ShouldBe(15);
  }

  [Fact]
  public void GetNextOccurrence_WithValidWeeklyCron_ReturnsNextOccurrence()
  {
    // Arrange - Every Monday at 10:00 AM UTC
    const string cronExpression = "0 0 10 ? * MON";
    var after = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc); // Monday

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Hour.ShouldBe(10);
    result.Value.Minute.ShouldBe(0);
    result.Value.DayOfWeek.ShouldBe(DayOfWeek.Monday);
  }

  [Fact]
  public void GetNextOccurrence_WithValidMonthlyCron_ReturnsNextOccurrence()
  {
    // Arrange - First day of every month at midnight UTC
    const string cronExpression = "0 0 0 1 * ?";
    var after = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Day.ShouldBe(1);
    result.Value.Month.ShouldBe(2); // Next month
    result.Value.Hour.ShouldBe(0);
  }

  [Fact]
  public void GetNextOccurrence_WithTimezone_ReturnsUtcTime()
  {
    // Arrange - Every day at 9:00 AM Moscow time
    const string cronExpression = "0 0 9 * * ?";
    var after = new DateTime(2024, 1, 15, 4, 0, 0, DateTimeKind.Utc); // 7 AM Moscow
    const string timezoneId = "Europe/Moscow";

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, timezoneId);

    // Assert
    result.ShouldNotBeNull();
    // Result should be in UTC (9 AM Moscow = 6 AM UTC in winter)
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
  }

  [Fact]
  public void GetNextOccurrence_WithInvalidCronExpression_ReturnsNull()
  {
    // Arrange
    const string invalidCron = "invalid cron expression";
    var after = DateTime.UtcNow;

    // Act
    var result = _evaluator.GetNextOccurrence(invalidCron, after);

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void GetNextOccurrence_WithInvalidTimezone_ReturnsNull()
  {
    // Arrange
    const string cronExpression = "0 0 9 * * ?";
    var after = DateTime.UtcNow;
    const string invalidTimezone = "Invalid/Timezone";

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, invalidTimezone);

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void GetNextOccurrence_WithNullTimezone_UsesSystemTimezone()
  {
    // Arrange - When no timezone is specified, Quartz uses system timezone
    const string cronExpression = "0 0 9 * * ?";
    var after = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after);

    // Assert
    result.ShouldNotBeNull();
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    // Result is returned in UTC but calculated based on system timezone
  }

  [Fact]
  public void GetNextOccurrence_WithEmptyTimezone_UsesSystemTimezone()
  {
    // Arrange - When timezone is empty, Quartz uses system timezone
    const string cronExpression = "0 0 9 * * ?";
    var after = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, "");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    // Result is returned in UTC but calculated based on system timezone
  }

  [Fact]
  public void GetNextOccurrence_WithEveryMinuteCron_ReturnsNextMinute()
  {
    // Arrange - Every minute in UTC
    const string cronExpression = "0 * * * * ?";
    var after = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.GetNextOccurrence(cronExpression, after, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Hour.ShouldBe(10);
    result.Value.Minute.ShouldBe(31);
  }

  #endregion

  #region ShouldTriggerInWindow Tests

  [Fact]
  public void ShouldTriggerInWindow_WithOccurrenceInWindow_ReturnsOccurrenceTime()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Hour.ShouldBe(9);
    result.Value.Minute.ShouldBe(0);
    result.Value.Day.ShouldBe(15);
  }

  [Fact]
  public void ShouldTriggerInWindow_WithOccurrenceBeforeWindow_ReturnsNull()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithOccurrenceAfterWindow_ReturnsNull()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 6, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithOccurrenceAtWindowEnd_ReturnsOccurrenceTime()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Hour.ShouldBe(9);
  }

  [Fact]
  public void ShouldTriggerInWindow_WithOccurrenceAtWindowStart_ReturnsNull()
  {
    // Arrange - Every day at 9:00 AM UTC
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    // Should be null because condition is > windowStart (not >=)
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithTimezone_ConvertsCorrectly()
  {
    // Arrange - Every day at 9:00 AM Moscow time
    const string cronExpression = "0 0 9 * * ?";
    // Window: 5 AM - 7 AM UTC (8 AM - 10 AM Moscow in winter)
    var windowStart = new DateTime(2024, 1, 15, 5, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 7, 0, 0, DateTimeKind.Utc);
    const string timezoneId = "Europe/Moscow";

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, timezoneId);

    // Assert
    result.ShouldNotBeNull();
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
  }

  [Fact]
  public void ShouldTriggerInWindow_WithInvalidCronExpression_ReturnsNull()
  {
    // Arrange
    const string invalidCron = "invalid cron";
    var windowStart = DateTime.UtcNow;
    var windowEnd = DateTime.UtcNow.AddHours(1);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(invalidCron, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithInvalidTimezone_ReturnsNull()
  {
    // Arrange
    const string cronExpression = "0 0 9 * * ?";
    var windowStart = DateTime.UtcNow;
    var windowEnd = DateTime.UtcNow.AddHours(1);
    const string invalidTimezone = "Invalid/Timezone";

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, invalidTimezone);

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithNullTimezone_UsesSystemTimezone()
  {
    // Arrange - Every day at 14:00 (will be interpreted in system timezone)
    // System timezone is UTC+5, so 14:00 local = 9:00 UTC
    const string cronExpression = "0 0 14 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd);

    // Assert
    result.ShouldNotBeNull();
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    // Result is in UTC
  }

  [Fact]
  public void ShouldTriggerInWindow_WithEmptyTimezone_UsesSystemTimezone()
  {
    // Arrange - Every day at 14:00 (will be interpreted in system timezone)
    // System timezone is UTC+5, so 14:00 local = 9:00 UTC
    const string cronExpression = "0 0 14 * * ?";
    var windowStart = new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "");

    // Assert
    result.ShouldNotBeNull();
    result.Value.Kind.ShouldBe(DateTimeKind.Utc);
    // Result is in UTC
  }

  [Fact]
  public void ShouldTriggerInWindow_WithMultipleOccurrencesInWindow_ReturnsFirstOccurrence()
  {
    // Arrange - Every minute in UTC
    const string cronExpression = "0 * * * * ?";
    var windowStart = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 5, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldNotBeNull();
    // Should return first occurrence after windowStart
    result.Value.Hour.ShouldBe(10);
    result.Value.Minute.ShouldBe(1);
  }

  [Fact]
  public void ShouldTriggerInWindow_WithNoOccurrence_ReturnsNull()
  {
    // Arrange - Only on Feb 29 (not a leap year in 2023)
    const string cronExpression = "0 0 0 29 2 ?";
    var windowStart = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public void ShouldTriggerInWindow_WithVeryShortWindow_WorksCorrectly()
  {
    // Arrange - Every minute in UTC
    const string cronExpression = "0 * * * * ?";
    var windowStart = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 10, 0, 30, DateTimeKind.Utc);

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, "UTC");

    // Assert
    result.ShouldBeNull(); // Next occurrence is at 10:01, which is after window end
  }

  [Fact]
  public void ShouldTriggerInWindow_WithWeeklyCronAndTimezone_WorksCorrectly()
  {
    // Arrange - Every Monday at 9:00 AM Moscow time
    const string cronExpression = "0 0 9 ? * MON";
    // Jan 15, 2024 is Monday
    var windowStart = new DateTime(2024, 1, 15, 5, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 7, 0, 0, DateTimeKind.Utc);
    const string timezoneId = "Europe/Moscow";

    // Act
    var result = _evaluator.ShouldTriggerInWindow(cronExpression, windowStart, windowEnd, timezoneId);

    // Assert
    result.ShouldNotBeNull();
    result.Value.DayOfWeek.ShouldBe(DayOfWeek.Monday);
  }

  #endregion
}
