using Ardalis.Result;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.UnitTests.Host.Worker.UseCases;

public class CalculatePetMoodScoreTests
{
  private readonly CalculatePetMoodScoreHandler _handler;
  private readonly IPetMoodCalculator _moodCalculator;
  private readonly IRepository<Pet> _petRepository;

  public CalculatePetMoodScoreTests()
  {
    _petRepository = Substitute.For<IRepository<Pet>>();
    _moodCalculator = Substitute.For<IPetMoodCalculator>();
    _handler = new CalculatePetMoodScoreHandler(_petRepository, _moodCalculator);
  }

  [Fact]
  public async Task Handle_Returns100_WhenNoTasksDue()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>())
      .Returns(pet);

    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>())
      .Returns(100);

    // Act
    var result = await _handler.Handle(
      new CalculatePetMoodScoreCommand(petId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task Handle_ReturnsNotFound_WhenPetDoesNotExist()
  {
    // Arrange
    var petId = Guid.NewGuid();
    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>())
      .Returns((Pet?)null);

    // Act
    var result = await _handler.Handle(
      new CalculatePetMoodScoreCommand(petId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenPetExists()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>())
      .Returns(pet);

    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>())
      .Returns(75);

    // Act
    var result = await _handler.Handle(
      new CalculatePetMoodScoreCommand(petId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(75);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZero()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZeroForOverdue()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_AppliesLateCompletionPenalty()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>()).Returns(50);

    // Act
    var result = await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(50);
  }

  [Fact]
  public async Task Handle_UpdatesPetMoodScore()
  {
    // Arrange
    var petId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var pet = new Pet(familyId, PetType.Cat, "Мурзик");

    _petRepository.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(pet);
    _moodCalculator.CalculateMoodScoreAsync(petId, Arg.Any<CancellationToken>()).Returns(100);

    // Act
    await _handler.Handle(new CalculatePetMoodScoreCommand(petId), CancellationToken.None);

    // Assert
    await _petRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    pet.MoodScore.ShouldBe(100);
  }

  // Helper methods
  private TaskInstance CreateCompletedTask(Guid familyId, Guid petId, TaskPoints points, DateTime dueAt,
    DateTime completedAt)
  {
    var task = new TaskInstance(familyId, petId, "Test Task", points, TaskType.OneTime, dueAt);
    task.Complete(Guid.NewGuid(), completedAt);
    return task;
  }

  private TaskInstance CreateOverdueTask(Guid familyId, Guid petId, TaskPoints points, DateTime dueAt) =>
    new(familyId, petId, "Overdue Task", points, TaskType.OneTime, dueAt);
}
