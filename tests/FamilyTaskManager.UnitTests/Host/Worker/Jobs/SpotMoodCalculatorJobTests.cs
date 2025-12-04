using Ardalis.Result;
using FamilyTaskManager.Host.Modules.Worker.Jobs;
using FamilyTaskManager.UseCases.Spots;
using Quartz;

namespace FamilyTaskManager.UnitTests.Host.Worker.Jobs;

public class SpotMoodCalculatorJobTests
{
  private readonly IJobExecutionContext _context;
  private readonly SpotMoodCalculatorJob _job;
  private readonly ILogger<SpotMoodCalculatorJob> _logger;
  private readonly IMediator _mediator;

  public SpotMoodCalculatorJobTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<SpotMoodCalculatorJob>>();
    _job = new(_mediator, _logger);
    _context = Substitute.For<IJobExecutionContext>();
  }

  [Fact]
  public async Task Execute_CalculatesMood_ForAllSpots()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllSpotsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _job.Execute(_context);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Any<CalculateAllSpotsMoodCommand>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Execute_DoesNotThrow_WhenNoSpotsExist()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllSpotsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act & Assert
    await Should.NotThrowAsync(async () => await _job.Execute(_context));
  }

  [Fact]
  public async Task Execute_HandlesErrors_Gracefully()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllSpotsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Error("Database error"));

    // Act & Assert
    await Should.NotThrowAsync(async () => await _job.Execute(_context));
  }

  [Fact]
  public async Task Execute_CompletesSuccessfully_WhenCommandSucceeds()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllSpotsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _job.Execute(_context);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Any<CalculateAllSpotsMoodCommand>(),
      Arg.Any<CancellationToken>());
  }
}
