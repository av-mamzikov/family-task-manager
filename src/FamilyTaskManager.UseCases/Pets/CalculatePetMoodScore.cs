namespace FamilyTaskManager.UseCases.Pets;

public record CalculatePetMoodScoreCommand(Guid PetId) : ICommand<Result<int>>;

public class CalculatePetMoodScoreHandler(
  IRepository<Pet> petRepository,
  IRepository<TaskInstance> taskRepository) 
  : ICommandHandler<CalculatePetMoodScoreCommand, Result<int>>
{
  public async ValueTask<Result<int>> Handle(CalculatePetMoodScoreCommand request, CancellationToken cancellationToken)
  {
    var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
    if (pet == null)
    {
      return Result.NotFound($"Pet with ID {request.PetId} not found.");
    }

    // Get all tasks for this pet where dueAt <= now
    var spec = new TasksByPetSpec(request.PetId);
    var allTasks = await taskRepository.ListAsync(spec, cancellationToken);
    
    var now = DateTime.UtcNow;
    var dueTasks = allTasks.Where(t => t.DueAt <= now).ToList();

    if (dueTasks.Count() == 0)
    {
      // No tasks due yet, mood is 100
      pet.UpdateMoodScore(100);
      await petRepository.SaveChangesAsync(cancellationToken);
      return Result.Success(100);
    }

    // Calculate mood based on formula from TЗ section 2.2
    int maxPoints = dueTasks.Sum(t => t.Points);
    double effectiveSum = 0;

    const double kLate = 0.5; // Coefficient for late completion (50% of points)

    foreach (var task in dueTasks)
    {
      if (task.Status == TaskStatus.Completed && task.CompletedAt.HasValue)
      {
        if (task.CompletedAt.Value <= task.DueAt)
        {
          // Completed on time - full points
          effectiveSum += task.Points;
        }
        else
        {
          // Completed late - partial points
          effectiveSum += task.Points * kLate;
        }
      }
      else if (task.Status != TaskStatus.Completed && now > task.DueAt)
      {
        // Not completed and overdue - negative contribution
        var overdueTime = now - task.DueAt;
        var overdueDays = overdueTime.TotalDays;
        
        // f(overdueTime) grows from 0 to 1 as overdue increases
        // Using formula: min(1, overdueDays / 7) - reaches 1 after 7 days
        var f = Math.Min(1.0, overdueDays / 7.0);
        
        effectiveSum -= task.Points * f;
      }
    }

    // Calculate mood score
    double baseMood = maxPoints > 0 ? 100.0 * (effectiveSum / maxPoints) : 100.0;
    int moodScore = Math.Clamp((int)Math.Round(baseMood), 0, 100);

    pet.UpdateMoodScore(moodScore);
    await petRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(moodScore);
  }
}
