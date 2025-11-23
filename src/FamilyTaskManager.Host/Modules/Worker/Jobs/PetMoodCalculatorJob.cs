using FamilyTaskManager.UseCases.Pets;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
///   Job that recalculates mood scores for all pets.
///   Runs every 30 minutes.
///   Domain events handle the actual notification sending.
/// </summary>
[DisallowConcurrentExecution]
public class PetMoodCalculatorJob(
  IMediator mediator,
  ILogger<PetMoodCalculatorJob> logger) : IJob
{
  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("PetMoodCalculatorJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Simply call the UseCase - it handles all the logic
      // Domain events will handle notifications
      var result = await mediator.Send(new CalculateAllPetsMoodCommand(), context.CancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("PetMoodCalculatorJob completed successfully");
      }
      else
      {
        logger.LogWarning("PetMoodCalculatorJob completed with errors: {Errors}", result.Errors);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "PetMoodCalculatorJob failed");
      throw;
    }
  }
}
