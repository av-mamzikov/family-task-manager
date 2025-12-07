using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Core.Services;

public class SpotMoodCalculator(
  IAppRepository<SpotBowsing> spotAppRepository,
  IAppRepository<TaskInstance> taskAppRepository) : ISpotMoodCalculator
{
  public async Task<int> CalculateMoodScoreAsync(Guid spotId, CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(spotId, cancellationToken);
    if (spot == null) throw new ArgumentException($"Spot with ID {spotId} not found.");

    // Get all tasks for this Spot where dueAt <= now
    var spec = new TasksBySpotSpec(spotId);
    var allTasks = await taskAppRepository.ListAsync(spec, cancellationToken);

    var now = DateTime.UtcNow;
    var dueTasks = allTasks.Where(t => t.DueAt <= now).ToList();

    if (dueTasks.Count == 0)
      // No tasks due yet, mood is 100
      return 100;

    // Calculate mood based on formula from TЗ section 2.2
    var maxPoints = dueTasks.Sum(t => t.Points);
    double effectiveSum = 0;

    const double kLate = 0.5; // Coefficient for late completion (50% of points)

    foreach (var task in dueTasks)
      if (task.Status == TaskStatus.Completed && task.CompletedAt.HasValue)
      {
        if (task.CompletedAt.Value <= task.DueAt)
          // Completed on time - full points
          effectiveSum += task.Points;
        else
          // Completed late - partial points
          effectiveSum += task.Points * kLate;
      }
      else if ((task.Status == TaskStatus.Active || task.Status == TaskStatus.InProgress) && now > task.DueAt)
      {
        // Active or InProgress task and overdue - negative contribution
        var overdueTime = now - task.DueAt;
        var overdueDays = overdueTime.TotalDays;

        // f(overdueTime) grows from 0 to 1 as overdue increases
        // Using formula: min(1, overdueDays / 7) - reaches 1 after 7 days
        var f = Math.Min(1.0, overdueDays / 7.0);

        effectiveSum -= task.Points * f;
      }

    // Calculate mood score
    var baseMood = maxPoints > 0 ? 100.0 * (effectiveSum / maxPoints) : 100.0;
    var newMoodScore = Math.Clamp((int)Math.Round(baseMood), 0, 100);

    return newMoodScore;
  }
}
