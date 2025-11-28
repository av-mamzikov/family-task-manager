using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UnitTests.Services;

public class PetMoodCalculatorTests
{
  private readonly PetMoodCalculator _calculator;
  private readonly IRepository<Pet> _petRepository;
  private readonly IRepository<TaskInstance> _taskRepository;

  public PetMoodCalculatorTests()
  {
    _petRepository = Substitute.For<IRepository<Pet>>();
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _calculator = new PetMoodCalculator(_petRepository, _taskRepository);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns100_WhenNoTasksDue()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns([]);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(petId, CancellationToken.None);

    // Assert
    result.ShouldBe(100);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns100_WhenTaskCompletedOnTime()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddHours(-2);
    var completedAt = now.AddHours(-3); // Completed before due

    var tasks = new List<TaskInstance>
    {
      CreateCompletedTask(familyId, petId, 10, dueAt, completedAt)
    };

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(petId, CancellationToken.None);

    // Assert
    // On-time completion gives full points: effectiveSum = 10, maxPoints = 10
    // mood = 100 * (10 / 10) = 100
    result.ShouldBe(100);
  }

  [Fact]
  public async Task CalculateMoodScoreAsync_Returns50_WhenTaskCompletedLate()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddHours(-2);
    var completedAt = now.AddHours(-1); // Completed 1 hour late

    var tasks = new List<TaskInstance>
    {
      CreateCompletedTask(familyId, petId, 10, dueAt, completedAt)
    };

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(petId, CancellationToken.None);

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
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

    var now = DateTime.UtcNow;
    var dueAt = now.AddDays(-10); // Overdue by 10 days

    var tasks = new List<TaskInstance>
    {
      CreateOverdueTask(familyId, petId, 10, dueAt)
    };

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(petId, CancellationToken.None);

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
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

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
      CreateCompletedTask(familyId, petId, 10, task1DueAt, task1CompletedAt),
      CreateCompletedTask(familyId, petId, 10, task2DueAt, task2CompletedAt),
      CreateOverdueTask(familyId, petId, 10, task3DueAt)
    };

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _taskRepository.ListAsync(Arg.Any<TasksByPetSpec>(), Arg.Any<CancellationToken>()).Returns(tasks);

    // Act
    var result = await _calculator.CalculateMoodScoreAsync(petId, CancellationToken.None);

    // Assert
    // Task 1: on-time = +10
    // Task 2: late = +10 * 0.5 = +5  
    // Task 3: overdue 3.5 days = -10 * (3.5/7) = -5
    // effectiveSum = 10 + 5 - 5 = 10, maxPoints = 30
    // mood = 100 * (10 / 30) = 33.33, rounded to 33
    result.ShouldBe(33);
  }

  // Helper methods
  private TaskInstance CreateCompletedTask(Guid familyId, Guid petId, int points, DateTime dueAt, DateTime completedAt)
  {
    var task = new TaskInstance(familyId, petId, "Test Task", points, TaskType.OneTime, dueAt);
    task.Complete(Guid.NewGuid(), completedAt);
    return task;
  }

  private TaskInstance CreateOverdueTask(Guid familyId, Guid petId, int points, DateTime dueAt) =>
    new(familyId, petId, "Overdue Task", points, TaskType.OneTime, dueAt);
}
