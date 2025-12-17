using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UnitTests.Helpers;

namespace FamilyTaskManager.UnitTests.Services;

public class SpotMoodCalculatorTests
{
  private readonly SpotMoodCalculator _calculator;
  private readonly IAppRepository<Spot> _spotAppRepository;
  private readonly IAppRepository<TaskInstance> _taskAppRepository;

  public SpotMoodCalculatorTests()
  {
    _spotAppRepository = Substitute.For<IAppRepository<Spot>>();
    _taskAppRepository = Substitute.For<IAppRepository<TaskInstance>>();
    _calculator = new(_spotAppRepository, _taskAppRepository);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns100_WhenNoTasksDue()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _taskAppRepository.ListAsync(Arg.Any<TasksBySpotSpec>(), Arg.Any<CancellationToken>()).Returns([]);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(spotId, CancellationToken.None);

    // Assert
    result.ShouldBe(100);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns100_WhenTaskCompletedOnTime()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddHours(-2);
    var completedAt = now.AddHours(-3); // Completed before due

    var tasks = new List<TaskInstance>
    {
      CreateCompletedTask(familyId, spotId, new(2), dueAt, completedAt)
    };

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _taskAppRepository.ListAsync(Arg.Any<TasksBySpotSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(spotId, CancellationToken.None);

    // Assert
    // On-time completion gives full points: effectiveSum = 10, maxPoints = 10
    // mood = 100 * (10 / 10) = 100
    result.ShouldBe(100);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns50_WhenTaskCompletedLate()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddHours(-2);
    var completedAt = now.AddHours(-1); // Completed 1 hour late

    var tasks = new List<TaskInstance>
    {
      CreateCompletedTask(familyId, spotId, new(2), dueAt, completedAt)
    };

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _taskAppRepository.ListAsync(Arg.Any<TasksBySpotSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(spotId, CancellationToken.None);

    // Assert
    // Late completion gives 50% of points (kLate = 0.5)
    // effectiveSum = 10 * 0.5 = 5, maxPoints = 10
    // mood = 100 * (5 / 10) = 50
    result.ShouldBe(50);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns0_WhenTaskOverdueMoreThan7Days()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddDays(-10); // Overdue by 10 days

    var tasks = new List<TaskInstance>
    {
      CreateOverdueTask(familyId, spotId, new(2), dueAt)
    };

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _taskAppRepository.ListAsync(Arg.Any<TasksBySpotSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(spotId, CancellationToken.None);

    // Assert
    // Overdue > 7 days gives full negative penalty: f = 1.0
    // effectiveSum = -10 * 1.0 = -10, maxPoints = 10
    // mood = 100 * (-10 / 10) = -100, clamped to 0
    result.ShouldBe(0);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_CalculatesMixedScenariosCorrectly()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    var now = DateTime.UtcNow;

    // Task 1: Completed on time (10 points)
    var task1DueAt = now.AddHours(-5);
    var task1CompletedAt = now.AddHours(-6);

    // Task 2: Completed late (10 points)
    var task2DueAt = now.AddHours(-2);
    var task2CompletedAt = now.AddHours(-1);

    // Task 3: Overdue by 3.5 days (10 points)
    var task3DueAt = now.AddDays(-3.5);

    var tasks = new List<TaskInstance>
    {
      CreateCompletedTask(familyId, spotId, new(2), task1DueAt, task1CompletedAt),
      CreateCompletedTask(familyId, spotId, new(2), task2DueAt, task2CompletedAt),
      CreateOverdueTask(familyId, spotId, new(2), task3DueAt)
    };

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _taskAppRepository.ListAsync(Arg.Any<TasksBySpotSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(spotId, CancellationToken.None);

    // Assert
    // Task 1: on-time = +10
    // Task 2: late = +10 * 0.5 = +5  
    // Task 3: overdue 3.5 days = -10 * (3.5/7) = -5
    // effectiveSum = 10 + 5 - 5 = 10, maxPoints = 30
    // mood = 100 * (10 / 30) = 33.33, rounded to 33
    result.ShouldBe(33);
  }

  // Helper methods
  private TaskInstance CreateCompletedTask(Guid familyId, Guid spotId, TaskPoints points, DateTime dueAt,
    DateTime completedAt)
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Test Task", points, dueAt);
    var member = TestHelpers.CreateMemberWithUser();
    task.Complete(member, completedAt);
    return task;
  }

  private TaskInstance CreateOverdueTask(Guid familyId, Guid spotId, TaskPoints points, DateTime dueAt)
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    return new(spot, "Test Task", points, dueAt);
  }
}
