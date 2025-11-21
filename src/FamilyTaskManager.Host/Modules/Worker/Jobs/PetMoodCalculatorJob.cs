using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Notifications;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that recalculates mood scores for all pets.
/// Runs every 30 minutes.
/// </summary>
[DisallowConcurrentExecution]
public class PetMoodCalculatorJob : IJob
{
  private readonly IMediator _mediator;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<PetMoodCalculatorJob> _logger;

  public PetMoodCalculatorJob(
    IMediator mediator, 
    IServiceProvider serviceProvider,
    ILogger<PetMoodCalculatorJob> logger)
  {
    _mediator = mediator;
    _serviceProvider = serviceProvider;
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

      // Create a scope to resolve scoped services
      using (var scope = _serviceProvider.CreateScope())
      {
        var notificationService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        foreach (var pet in pets)
        {
          try
          {
            var result = await _mediator.Send(new CalculatePetMoodScoreCommand(pet.Id));

            if (result.IsSuccess)
            {
              updatedCount++;
              var moodResult = result.Value;
              
              _logger.LogDebug(
                "Updated mood score for pet {PetId} ({PetName}): {OldMood} -> {NewMood}",
                pet.Id, pet.Name, moodResult.OldMoodScore, moodResult.NewMoodScore);

              // Send notification if mood changed significantly (crossed critical thresholds)
              if (moodResult.HasChanged && ShouldNotifyMoodChange(moodResult.OldMoodScore, moodResult.NewMoodScore))
              {
                await notificationService.SendPetMoodChangedAsync(
                  pet.FamilyId, 
                  pet.Name, 
                  moodResult.NewMoodScore, 
                  context.CancellationToken);
                
                _logger.LogInformation(
                  "Sent mood change notification for pet {PetName}: {OldMood} -> {NewMood}",
                  pet.Name, moodResult.OldMoodScore, moodResult.NewMoodScore);
              }
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

  /// <summary>
  /// Determines if a mood change is significant enough to send a notification.
  /// Notifies when crossing critical thresholds: < 20, < 50, or >= 80
  /// </summary>
  private static bool ShouldNotifyMoodChange(int oldMood, int newMood)
  {
    // Define critical thresholds
    const int criticalLow = 20;
    const int warningLow = 50;
    const int excellent = 80;

    // Check if crossed critical low threshold (< 20)
    if ((oldMood >= criticalLow && newMood < criticalLow) ||
        (oldMood < criticalLow && newMood >= criticalLow))
    {
      return true;
    }

    // Check if crossed warning threshold (< 50)
    if ((oldMood >= warningLow && newMood < warningLow) ||
        (oldMood < warningLow && newMood >= warningLow))
    {
      return true;
    }

    // Check if crossed excellent threshold (>= 80)
    if ((oldMood < excellent && newMood >= excellent) ||
        (oldMood >= excellent && newMood < excellent))
    {
      return true;
    }

    return false;
  }
}
