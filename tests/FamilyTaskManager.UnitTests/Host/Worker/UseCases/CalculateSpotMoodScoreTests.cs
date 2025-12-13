using Ardalis.Result;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.UseCases.Features.SpotManagement.Commands;

namespace FamilyTaskManager.UnitTests.Host.Worker.UseCases;

public class CalculateSpotMoodScoreTests
{
  private readonly CalculateSpotMoodScoreHandler _handler;
  private readonly ISpotMoodCalculator _moodCalculator;
  private readonly IAppRepository<Spot> _SpotAppRepository;

  public CalculateSpotMoodScoreTests()
  {
    _SpotAppRepository = Substitute.For<IAppRepository<Spot>>();
    _moodCalculator = Substitute.For<ISpotMoodCalculator>();
    _handler = new(_SpotAppRepository, _moodCalculator);
  }

  [Fact]
  public async Task Handle_Returns100_WhenNoTasksDue()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var Spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>())
      .Returns(Spot);

    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>())
      .Returns(100);

    // Act
    var result = await _handler.Handle(
      new(SpotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task Handle_ReturnsNotFound_WhenSpotDoesNotExist()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>())
      .Returns((Spot?)null);

    // Act
    var result = await _handler.Handle(
      new(SpotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenSpotExists()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var Spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>())
      .Returns(Spot);

    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>())
      .Returns(75);

    // Act
    var result = await _handler.Handle(
      new(SpotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(75);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZero()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var Spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>()).Returns(Spot);
    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new(SpotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZeroForOverdue()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var Spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>()).Returns(Spot);
    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new(SpotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_AppliesLateCompletionPenalty()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var Spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>()).Returns(Spot);
    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>()).Returns(50);

    // Act
    var result = await _handler.Handle(new(SpotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(50);
  }

  [Fact]
  public async Task Handle_UpdatesSpotMoodScore()
  {
    // Arrange
    var SpotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var Spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    _SpotAppRepository.GetByIdAsync(SpotId, Arg.Any<CancellationToken>()).Returns(Spot);
    _moodCalculator.CalculateMoodScoreAsync(SpotId, Arg.Any<CancellationToken>()).Returns(100);

    // Act
    await _handler.Handle(new(SpotId), CancellationToken.None);

    // Assert
    await _SpotAppRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    Spot.MoodScore.ShouldBe(100);
  }
}
