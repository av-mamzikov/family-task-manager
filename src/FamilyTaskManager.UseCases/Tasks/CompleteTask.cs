using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Tasks;

public record CompleteTaskCommand(Guid TaskId, Guid UserId) : ICommand<Result>;

public class CompleteTaskHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository,
  IAppRepository<Spot> SpotAppRepository,
  ISpotMoodCalculator moodCalculator) : ICommandHandler<CompleteTaskCommand, Result>
{
  public async ValueTask<Result> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
  {
    // Get task
    var task = await taskAppRepository.GetByIdAsync(command.TaskId, cancellationToken);
    if (task == null)
    {
      return Result.NotFound("Task not found");
    }

    if (task.Status == TaskStatus.Completed)
    {
      return Result.Error("Task is already completed");
    }

    // Get family with members (including User for event)
    var familySpec = new GetFamilyWithMembersAndUsersSpec(task.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Family not found");
    }

    // Find member
    var member = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.IsActive);
    if (member == null)
    {
      return Result.Error("User is not a member of this family");
    }

    // Check if task is in progress and if current user is the one who started it
    if (task.Status == TaskStatus.InProgress && task.StartedByMemberId.HasValue &&
        task.StartedByMemberId.Value != member.Id)
      return Result.Error("Only the user who started this task can complete it");

    // Complete task (registers TaskCompletedEvent with member.User.Name)
    task.Complete(member, DateTime.UtcNow);

    // Add points to member
    member.AddPoints(task.Points);

    // Update both entities (domain events will be dispatched automatically)
    await taskAppRepository.UpdateAsync(task, cancellationToken);
    await familyAppRepository.UpdateAsync(family, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the Spot
    var Spot = await SpotAppRepository.GetByIdAsync(task.SpotId, cancellationToken);
    if (Spot != null)
    {
      var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(task.SpotId, cancellationToken);
      Spot.UpdateMoodScore(newMoodScore);
      await SpotAppRepository.SaveChangesAsync(cancellationToken);
    }

    return Result.Success();
  }
}
