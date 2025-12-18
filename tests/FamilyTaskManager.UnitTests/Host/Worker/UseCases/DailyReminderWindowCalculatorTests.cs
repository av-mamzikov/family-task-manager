using FamilyTaskManager.UseCases.Features.TasksManagement.Services;

namespace FamilyTaskManager.UnitTests.Host.Worker.UseCases;

public class DailyReminderWindowCalculatorTests
{
  [Fact]
  public void CrossedLocalTimeBetween_ReturnsTrue_WhenCrossed19()
  {
    // Arrange
    var tz = Substitute.For<ITimeZoneService>();
    tz.ConvertFromUtc(Arg.Any<DateTime>(), Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<DateTime>(0));

    var prev = new DateTime(2025, 12, 18, 18, 50, 0, DateTimeKind.Utc);
    var current = new DateTime(2025, 12, 18, 19, 05, 0, DateTimeKind.Utc);

    // Act
    var crossed = DailyReminderWindowCalculator.CrossedLocalTimeBetween(
      prev,
      current,
      "UTC",
      tz,
      19,
      0);

    // Assert
    crossed.ShouldBeTrue();
  }

  [Fact]
  public void CrossedLocalTimeBetween_ReturnsFalse_WhenDidNotCross19()
  {
    // Arrange
    var tz = Substitute.For<ITimeZoneService>();
    tz.ConvertFromUtc(Arg.Any<DateTime>(), Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<DateTime>(0));

    var prev = new DateTime(2025, 12, 18, 19, 10, 0, DateTimeKind.Utc);
    var current = new DateTime(2025, 12, 18, 19, 25, 0, DateTimeKind.Utc);

    // Act
    var crossed = DailyReminderWindowCalculator.CrossedLocalTimeBetween(
      prev,
      current,
      "UTC",
      tz,
      19,
      0);

    // Assert
    crossed.ShouldBeFalse();
  }

  [Fact]
  public void CrossedLocalTimeBetween_ReturnsFalse_WhenPreviousIsNull()
  {
    // Arrange
    var tz = Substitute.For<ITimeZoneService>();
    tz.ConvertFromUtc(Arg.Any<DateTime>(), Arg.Any<string>()).Returns(callInfo => callInfo.ArgAt<DateTime>(0));

    var current = new DateTime(2025, 12, 18, 19, 0, 0, DateTimeKind.Utc);

    // Act
    var crossed = DailyReminderWindowCalculator.CrossedLocalTimeBetween(
      null,
      current,
      "UTC",
      tz,
      19,
      0);

    // Assert
    crossed.ShouldBeFalse();
  }
}
