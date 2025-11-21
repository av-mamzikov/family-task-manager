using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that recalculates mood scores for all pets.
/// Runs every 30 minutes.
/// </summary>
[DisallowConcurrentExecution]
public class PetMoodCalculatorJob : IJob
{
  private readonly IMediator _mediator;
  private readonly ILogger<PetMoodCalculatorJob> _logger;

  public PetMoodCalculatorJob(IMediator mediator, ILogger<PetMoodCalculatorJob> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    _logger.LogInformation("PetMoodCalculatorJob started at {Time}", DateTimeOffset.UtcNow);

    try
    {
      // Get all pets
      var petsResult = await _mediator.Send(new GetAllPetsQuery());
      
      if (!petsResult.IsSuccess)
      {
        _logger.LogWarning("Failed to get pets: {Error}", petsResult.Errors);
        return;
      }

      var pets = petsResult.Value;
      _logger.LogInformation("Found {Count} pets to update mood scores", pets.Count);

      var updatedCount = 0;

      foreach (var pet in pets)
      {
        try
        {
          var result = await _mediator.Send(new CalculatePetMoodScoreCommand(pet.Id));

          if (result.IsSuccess)
          {
            updatedCount++;
            _logger.LogDebug(
              "Updated mood score for pet {PetId} ({PetName}): {MoodScore}",
              pet.Id, pet.Name, result.Value);
          }
          else
          {
            _logger.LogWarning(
              "Failed to update mood score for pet {PetId}: {Error}",
              pet.Id, string.Join(", ", result.Errors));
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, 
            "Error calculating mood for pet {PetId}", 
            pet.Id);
        }
      }

      _logger.LogInformation(
        "PetMoodCalculatorJob completed. Updated {UpdatedCount} pet mood scores", 
        updatedCount);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in PetMoodCalculatorJob");
    }
  }
}
