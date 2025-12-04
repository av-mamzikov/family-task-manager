namespace FamilyTaskManager.Core.Services;

public interface ISpotMoodCalculator
{
  /// <summary>
  ///   Calculates the mood score for a spot based on its tasks
  /// </summary>
  /// <param name="spotId">The spot ID to calculate mood for</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The calculated mood score (0-100)</returns>
  Task<int> CalculateMoodScoreAsync(Guid spotId, CancellationToken cancellationToken);
}
