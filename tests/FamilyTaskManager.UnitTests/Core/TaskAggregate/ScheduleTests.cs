using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.UnitTests.Core.TaskAggregate;

public class ScheduleTests
{
  [Fact]
  public void CreateDaily_CreatesValidSchedule()
  {
    // Arrange
    var time = new TimeOnly(10, 30);

    // Act
    var result = Schedule.CreateDaily(time);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Type.ShouldBe(ScheduleType.Daily);
    result.Value.Time.ShouldBe(time);
    result.Value.DayOfWeek.ShouldBeNull();
    result.Value.DayOfMonth.ShouldBeNull();
  }

  [Fact]
  public void CreateWeekly_CreatesValidSchedule()
  {
    // Arrange
    var time = new TimeOnly(15, 0);
    var dayOfWeek = DayOfWeek.Monday;

    // Act
    var result = Schedule.CreateWeekly(time, dayOfWeek);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Type.ShouldBe(ScheduleType.Weekly);
    result.Value.Time.ShouldBe(time);
    result.Value.DayOfWeek.ShouldBe(dayOfWeek);
    result.Value.DayOfMonth.ShouldBeNull();
  }

  [Fact]
  public void CreateMonthly_WithValidDay_CreatesValidSchedule()
  {
    // Arrange
    var time = new TimeOnly(12, 0);
    var dayOfMonth = 15;

    // Act
    var result = Schedule.CreateMonthly(time, dayOfMonth);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Type.ShouldBe(ScheduleType.Monthly);
    result.Value.Time.ShouldBe(time);
    result.Value.DayOfWeek.ShouldBeNull();
    result.Value.DayOfMonth.ShouldBe(dayOfMonth);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(32)]
  [InlineData(-1)]
  public void CreateMonthly_WithInvalidDay_ReturnsError(int invalidDay)
  {
    // Arrange
    var time = new TimeOnly(12, 0);

    // Act
    var result = Schedule.CreateMonthly(time, invalidDay);

    // Assert
    result.IsSuccess.ShouldBeFalse();
  }

  [Fact]
  public void CreateWorkdays_CreatesValidSchedule()
  {
    // Arrange
    var time = new TimeOnly(9, 0);

    // Act
    var result = Schedule.CreateWorkdays(time);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Type.ShouldBe(ScheduleType.Workdays);
    result.Value.Time.ShouldBe(time);
  }

  [Fact]
  public void CreateWeekends_CreatesValidSchedule()
  {
    // Arrange
    var time = new TimeOnly(11, 0);

    // Act
    var result = Schedule.CreateWeekends(time);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Type.ShouldBe(ScheduleType.Weekends);
    result.Value.Time.ShouldBe(time);
  }

  [Fact]
  public void GetNextOccurrence_Daily_ReturnsNextDay()
  {
    // Arrange
    var schedule = Schedule.CreateDaily(new TimeOnly(10, 0)).Value;
    var after = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc); // After 10:00
    var timeZone = TimeZoneInfo.Utc;

    // Act
    var next = schedule.GetNextOccurrence(after, timeZone);

    // Assert
    next.ShouldNotBeNull();
    next.Value.Date.ShouldBe(new DateTime(2024, 1, 16)); // Next day
    next.Value.Hour.ShouldBe(10);
    next.Value.Minute.ShouldBe(0);
  }

  [Fact]
  public void GetNextOccurrence_Weekly_ReturnsNextWeek()
  {
    // Arrange
    var schedule = Schedule.CreateWeekly(new TimeOnly(15, 0), DayOfWeek.Monday).Value;
    var after = new DateTime(2024, 1, 15, 16, 0, 0, DateTimeKind.Utc); // Monday after 15:00
    var timeZone = TimeZoneInfo.Utc;

    // Act
    var next = schedule.GetNextOccurrence(after, timeZone);

    // Assert
    next.ShouldNotBeNull();
    next.Value.DayOfWeek.ShouldBe(DayOfWeek.Monday);
    next.Value.Hour.ShouldBe(15);
  }

  [Fact]
  public void ShouldTriggerInWindow_WithinWindow_ReturnsTime()
  {
    // Arrange
    var schedule = Schedule.CreateDaily(new TimeOnly(10, 0)).Value;
    var windowStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
    var timeZone = TimeZoneInfo.Utc;

    // Act
    var triggerTime = schedule.ShouldTriggerInWindow(windowStart, windowEnd, timeZone);

    // Assert
    triggerTime.ShouldNotBeNull();
    triggerTime.Value.Hour.ShouldBe(10);
  }

  [Fact]
  public void ShouldTriggerInWindow_OutsideWindow_ReturnsNull()
  {
    // Arrange
    var schedule = Schedule.CreateDaily(new TimeOnly(14, 0)).Value;
    var windowStart = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    var windowEnd = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
    var timeZone = TimeZoneInfo.Utc;

    // Act
    var triggerTime = schedule.ShouldTriggerInWindow(windowStart, windowEnd, timeZone);

    // Assert
    triggerTime.ShouldBeNull();
  }

  [Fact]
  public void ValueObject_Equality_WorksCorrectly()
  {
    // Arrange
    var schedule1 = Schedule.CreateDaily(new TimeOnly(10, 0)).Value;
    var schedule2 = Schedule.CreateDaily(new TimeOnly(10, 0)).Value;
    var schedule3 = Schedule.CreateDaily(new TimeOnly(11, 0)).Value;

    // Assert
    schedule1.ShouldBe(schedule2);
    schedule1.ShouldNotBe(schedule3);
  }
}
