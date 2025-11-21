using Quartz;

namespace FamilyTaskManager.WorkerTests;

public class CronExpressionTests
{
    [Theory]
    [InlineData("0 0 9 * * ?", "2025-11-21 08:00:00", "2025-11-21 09:00:00")]
    [InlineData("0 0 9 * * ?", "2025-11-21 09:00:00", "2025-11-22 09:00:00")]
    [InlineData("0 */15 * * * ?", "2025-11-21 12:00:00", "2025-11-21 12:15:00")]
    [InlineData("0 */15 * * * ?", "2025-11-21 12:15:00", "2025-11-21 12:30:00")]
    [InlineData("0 0 9 */5 * ?", "2025-11-21 09:00:00", "2025-11-26 09:00:00")]
    [InlineData("0 0 20 * * ?", "2025-11-21 19:00:00", "2025-11-21 20:00:00")]
    [InlineData("0 * * * * ?", "2025-11-21 12:00:00", "2025-11-21 12:01:00")]
    public void CronExpression_CalculatesNextOccurrence_Correctly(
        string cronExpression, string currentTime, string expectedNext)
    {
        // Arrange
        var cron = new CronExpression(cronExpression);
        var current = DateTime.Parse(currentTime);
        var expected = DateTime.Parse(expectedNext);

        // Act
        var next = cron.GetTimeAfter(DateTimeOffset.Parse(currentTime));

        // Assert
        next.ShouldNotBeNull();
        next.Value.DateTime.ShouldBe(expected, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("0 0 9 * * ?")]      // Daily at 9:00
    [InlineData("0 */15 * * * ?")]   // Every 15 minutes
    [InlineData("0 0 9,20 * * ?")]   // Daily at 9:00 and 20:00
    [InlineData("0 0 9 * * MON")]    // Every Monday at 9:00
    [InlineData("0 0 9 1 * ?")]      // First day of month at 9:00
    public void CronExpression_ValidatesCorrectly_ForValidExpressions(string cronExpression)
    {
        // Act & Assert
        Should.NotThrow(() => new CronExpression(cronExpression));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("* * * *")]          // Too few fields
    [InlineData("0 0 25 * * ?")]     // Invalid hour (25)
    [InlineData("0 60 * * * ?")]     // Invalid minute (60)
    public void CronExpression_ThrowsException_ForInvalidExpressions(string cronExpression)
    {
        // Act & Assert
        Should.Throw<Exception>(() => new CronExpression(cronExpression));
    }

    [Fact]
    public void CronExpression_EveryMinute_WorksCorrectly()
    {
        // Arrange
        var cron = new CronExpression("0 * * * * ?");
        var start = new DateTime(2025, 11, 21, 12, 0, 0);

        // Act
        var occurrences = new List<DateTime>();
        var current = DateTimeOffset.Parse(start.ToString());
        
        for (int i = 0; i < 5; i++)
        {
            var next = cron.GetTimeAfter(current);
            next.ShouldNotBeNull();
            occurrences.Add(next.Value.DateTime);
            current = next.Value;
        }

        // Assert
        occurrences.Count.ShouldBe(5);
        occurrences[0].ShouldBe(new DateTime(2025, 11, 21, 12, 1, 0));
        occurrences[1].ShouldBe(new DateTime(2025, 11, 21, 12, 2, 0));
        occurrences[2].ShouldBe(new DateTime(2025, 11, 21, 12, 3, 0));
        occurrences[3].ShouldBe(new DateTime(2025, 11, 21, 12, 4, 0));
        occurrences[4].ShouldBe(new DateTime(2025, 11, 21, 12, 5, 0));
    }

    [Fact]
    public void CronExpression_Every5Days_WorksCorrectly()
    {
        // Arrange
        var cron = new CronExpression("0 0 9 */5 * ?");
        var start = new DateTime(2025, 11, 1, 9, 0, 0);

        // Act
        var occurrences = new List<DateTime>();
        var current = DateTimeOffset.Parse(start.ToString());
        
        for (int i = 0; i < 4; i++)
        {
            var next = cron.GetTimeAfter(current);
            next.ShouldNotBeNull();
            occurrences.Add(next.Value.DateTime);
            current = next.Value;
        }

        // Assert
        occurrences.Count.ShouldBe(4);
        occurrences[0].ShouldBe(new DateTime(2025, 11, 6, 9, 0, 0));
        occurrences[1].ShouldBe(new DateTime(2025, 11, 11, 9, 0, 0));
        occurrences[2].ShouldBe(new DateTime(2025, 11, 16, 9, 0, 0));
        occurrences[3].ShouldBe(new DateTime(2025, 11, 21, 9, 0, 0));
    }

    [Fact]
    public void CronExpression_TwiceDaily_WorksCorrectly()
    {
        // Arrange
        var cron = new CronExpression("0 0 9,20 * * ?");
        var start = new DateTime(2025, 11, 21, 8, 0, 0);

        // Act
        var next1 = cron.GetTimeAfter(DateTimeOffset.Parse(start.ToString()));
        var next2 = cron.GetTimeAfter(next1!.Value);
        var next3 = cron.GetTimeAfter(next2!.Value);

        // Assert
        next1!.Value.DateTime.ShouldBe(new DateTime(2025, 11, 21, 9, 0, 0));
        next2!.Value.DateTime.ShouldBe(new DateTime(2025, 11, 21, 20, 0, 0));
        next3!.Value.DateTime.ShouldBe(new DateTime(2025, 11, 22, 9, 0, 0));
    }

    [Theory]
    [InlineData("0 0 9 * * ?", "Feed cat - daily at 9:00")]
    [InlineData("0 0 9,20 * * ?", "Feed cat - twice daily")]
    [InlineData("0 0 20 * * ?", "Clean litter box - daily at 20:00")]
    [InlineData("0 0 9 */5 * ?", "Vet checkup - every 5 days")]
    public void CronExpression_CommonPetTaskSchedules_AreValid(string cronExpression, string description)
    {
        // Act & Assert
        Should.NotThrow(() =>
        {
            var cron = new CronExpression(cronExpression);
            var next = cron.GetTimeAfter(DateTimeOffset.UtcNow);
            next.ShouldNotBeNull($"Schedule '{description}' should have a next occurrence");
        });
    }
}
