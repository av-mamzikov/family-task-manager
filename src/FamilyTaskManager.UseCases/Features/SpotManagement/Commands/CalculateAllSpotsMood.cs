using FamilyTaskManager.UseCases.Features.SpotManagement.Queries;

namespace FamilyTaskManager.UseCases.Features.SpotManagement.Commands;

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
    var spotsResult = await mediator.Send(new GetAllSpotsQuery(), cancellationToken);

    if (!spotsResult.IsSuccess) return Result.Error("Failed to retrieve Spots");

    var spots = spotsResult.Value;

    // Calculate mood for each spot (will register domain events if needed)
    foreach (var spot in spots) await mediator.Send(new CalculateSpotMoodScoreCommand(spot.Id), cancellationToken);

    return Result.Success();
  }
}
