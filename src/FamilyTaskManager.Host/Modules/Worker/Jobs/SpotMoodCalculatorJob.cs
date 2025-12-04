using FamilyTaskManager.UseCases.Spots;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
///   Job that recalculates mood scores for all Spots.
///   Runs every 30 minutes.
///   Domain events handle the actual notification sending.
/// </summary>
[DisallowConcurrentExecution]
public class SpotMoodCalculatorJob(
  IMediator mediator,
  ILogger<SpotMoodCalculatorJob> logger) : IJob
{
  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("SpotMoodCalculatorJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Simply call the UseCase - it handles all the logic
      // Domain events will handle notifications
      var result = await mediator.Send(new CalculateAllSpotsMoodCommand(), context.CancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("SpotMoodCalculatorJob completed successfully");
      }
      else
      {
        logger.LogWarning("SpotMoodCalculatorJob completed with errors: {Errors}", result.Errors);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "SpotMoodCalculatorJob failed");
      throw;
    }
  }
}
