using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Core.Services;

public class PetMoodCalculator(
  IRepository<Pet> petRepository,
  IRepository<TaskInstance> taskRepository) : IPetMoodCalculator
{
  public async Task<int> CalculateMoodScoreAsync(Guid petId, CancellationToken cancellationToken)
  {
    var pet = await petRepository.GetByIdAsync(petId, cancellationToken);
    if (pet == null) throw new ArgumentException($"Pet with ID {petId} not found.");

    // Get all tasks for this pet where dueAt <= now
    var spec = new TasksByPetSpec(petId);
    var allTasks = await taskRepository.ListAsync(spec, cancellationToken);

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
