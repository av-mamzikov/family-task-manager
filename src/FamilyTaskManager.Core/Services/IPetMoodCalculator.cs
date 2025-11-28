namespace FamilyTaskManager.Core.Services;

public interface IPetMoodCalculator
{
  /// <summary>
  ///   Calculates the mood score for a pet based on their tasks
  /// </summary>
  /// <param name="petId">The pet ID to calculate mood for</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The calculated mood score (0-100)</returns>
  Task<int> CalculateMoodScoreAsync(Guid petId, CancellationToken cancellationToken);
}
