using Ardalis.Result;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.UseCases.Features.SpotManagement.Commands;

namespace FamilyTaskManager.UnitTests.Host.Worker.UseCases;

public class CalculateSpotMoodScoreTests
{
  private readonly CalculateSpotMoodScoreHandler _handler;
  private readonly ISpotMoodCalculator _moodCalculator;
  private readonly IAppRepository<Spot> _spotAppRepository;

  public CalculateSpotMoodScoreTests()
  {
    _spotAppRepository = Substitute.For<IAppRepository<Spot>>();
    _moodCalculator = Substitute.For<ISpotMoodCalculator>();
    _handler = new(_spotAppRepository, _moodCalculator);
  }

  [Fact]
  public async Task Handle_Returns100_WhenNoTasksDue()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>())
      .Returns(spot);

    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>())
      .Returns(100);

    // Act
    var result = await _handler.Handle(
      new(spotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task Handle_ReturnsNotFound_WhenSpotDoesNotExist()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>())
      .Returns((Spot?)null);

    // Act
    var result = await _handler.Handle(
      new(spotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenSpotExists()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>())
      .Returns(spot);

    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>())
      .Returns(75);

    // Act
    var result = await _handler.Handle(
      new(spotId),
      CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(75);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZero()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new(spotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_ReturnsCalculatorResult_WhenCalculatorReturnsZeroForOverdue()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>()).Returns(0);

    // Act
    var result = await _handler.Handle(new(spotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(0);
  }

  [Fact]
  public async Task Handle_AppliesLateCompletionPenalty()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>()).Returns(50);

    // Act
    var result = await _handler.Handle(new(spotId), CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.NewMoodScore.ShouldBe(50);
  }

  [Fact]
  public async Task Handle_UpdatesSpotMoodScore()
  {
    // Arrange
    var spotId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var spot = new Spot(familyId, SpotType.Cat, "Мурзик");

    _spotAppRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
    _moodCalculator.CalculateMoodScoreAsync(spotId, Arg.Any<CancellationToken>()).Returns(100);

    // Act
    await _handler.Handle(new(spotId), CancellationToken.None);

    // Assert
    await _spotAppRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    spot.MoodScore.ShouldBe(100);
  }
}
