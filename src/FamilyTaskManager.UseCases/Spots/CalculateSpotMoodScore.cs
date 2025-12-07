using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Spots;

public record SpotMoodScoreResult(int OldMoodScore, int NewMoodScore, bool HasChanged);

public record CalculateSpotMoodScoreCommand(Guid SpotId) : ICommand<Result<SpotMoodScoreResult>>;

public class CalculateSpotMoodScoreHandler(
  IAppRepository<Spot> spotAppRepository,
  ISpotMoodCalculator moodCalculator)
  : ICommandHandler<CalculateSpotMoodScoreCommand, Result<SpotMoodScoreResult>>
{
  public async ValueTask<Result<SpotMoodScoreResult>> Handle(CalculateSpotMoodScoreCommand request,
    CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(request.SpotId, cancellationToken);
    if (spot == null) return Result.NotFound($"Spot with ID {request.SpotId} not found.");

    var oldMoodScore = spot.MoodScore;
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(request.SpotId, cancellationToken);

    spot.UpdateMoodScore(newMoodScore);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(new SpotMoodScoreResult(oldMoodScore, newMoodScore, oldMoodScore != newMoodScore));
  }
}
