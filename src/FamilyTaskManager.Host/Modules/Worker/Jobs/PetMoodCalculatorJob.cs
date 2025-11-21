using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that recalculates mood scores for all pets.
/// Runs every 30 minutes.
/// Domain events handle the actual notification sending.
/// </summary>
[DisallowConcurrentExecution]
public class PetMoodCalculatorJob : IJob
{
  private readonly IMediator _mediator;
  private readonly ILogger<PetMoodCalculatorJob> _logger;

  public PetMoodCalculatorJob(
    IMediator mediator,
    ILogger<PetMoodCalculatorJob> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    _logger.LogInformation("PetMoodCalculatorJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Simply call the UseCase - it handles all the logic
      // Domain events will handle notifications
      var result = await _mediator.Send(new CalculateAllPetsMoodCommand(), context.CancellationToken);

      if (result.IsSuccess)
      {
        _logger.LogInformation("PetMoodCalculatorJob completed successfully");
      }
      else
      {
        _logger.LogWarning("PetMoodCalculatorJob completed with errors: {Errors}", result.Errors);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "PetMoodCalculatorJob failed");
      throw;
    }
  }
}
