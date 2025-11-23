using Ardalis.Result;
using FamilyTaskManager.Host.Modules.Worker.Jobs;
using FamilyTaskManager.UseCases.Pets;
using Quartz;

namespace FamilyTaskManager.UnitTests.Host.Worker.Jobs;

public class PetMoodCalculatorJobTests
{
  private readonly IJobExecutionContext _context;
  private readonly PetMoodCalculatorJob _job;
  private readonly ILogger<PetMoodCalculatorJob> _logger;
  private readonly IMediator _mediator;

  public PetMoodCalculatorJobTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<PetMoodCalculatorJob>>();
    _job = new PetMoodCalculatorJob(_mediator, _logger);
    _context = Substitute.For<IJobExecutionContext>();
  }

  [Fact]
  public async Task Execute_CalculatesMood_ForAllPets()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllPetsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _job.Execute(_context);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Any<CalculateAllPetsMoodCommand>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Execute_DoesNotThrow_WhenNoPetsExist()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllPetsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act & Assert
    await Should.NotThrowAsync(async () => await _job.Execute(_context));
  }

  [Fact]
  public async Task Execute_HandlesErrors_Gracefully()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllPetsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Error("Database error"));

    // Act & Assert
    await Should.NotThrowAsync(async () => await _job.Execute(_context));
  }

  [Fact]
  public async Task Execute_CompletesSuccessfully_WhenCommandSucceeds()
  {
    // Arrange
    _mediator.Send(Arg.Any<CalculateAllPetsMoodCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _job.Execute(_context);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Any<CalculateAllPetsMoodCommand>(),
      Arg.Any<CancellationToken>());
  }
}
