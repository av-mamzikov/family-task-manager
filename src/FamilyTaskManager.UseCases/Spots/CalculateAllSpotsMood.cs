namespace FamilyTaskManager.UseCases.Spots;

/// <summary>
///   Command to calculate mood scores for all Spots
/// </summary>
public record CalculateAllSpotsMoodCommand : ICommand<Result>;

public class CalculateAllSpotsMoodHandler(IMediator mediator)
  : ICommandHandler<CalculateAllSpotsMoodCommand, Result>
{
  public async ValueTask<Result> Handle(CalculateAllSpotsMoodCommand command, CancellationToken cancellationToken)
  {
    // Get all Spots
    var SpotsResult = await mediator.Send(new GetAllSpotsQuery(), cancellationToken);

    if (!SpotsResult.IsSuccess)
    {
      return Result.Error("Failed to retrieve Spots");
    }

    var Spots = SpotsResult.Value;

    // Calculate mood for each spot (will register domain events if needed)
    foreach (var Spot in Spots)
    {
      await mediator.Send(new CalculateSpotMoodScoreCommand(Spot.Id), cancellationToken);
    }

    return Result.Success();
  }
}
