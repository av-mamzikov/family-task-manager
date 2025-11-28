using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskTemplate : EntityBase<TaskTemplate, Guid>, IAggregateRoot
{
  private TaskTemplate()
  {
  }

  public TaskTemplate(Guid familyId, Guid petId, string title, int points, string schedule, TimeSpan dueDuration,
    Guid createdBy)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(petId);
    Guard.Against.Default(createdBy);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.OutOfRange(points, nameof(points), 1, 100);
    Guard.Against.NullOrWhiteSpace(schedule);
    Guard.Against.OutOfRange(dueDuration.TotalHours, nameof(dueDuration), 1, 720); // 1 hour to 30 days

    FamilyId = familyId;
    PetId = petId;
    Title = title.Trim();
    Points = points;
    Schedule = schedule.Trim();
    DueDuration = dueDuration;
    CreatedBy = createdBy;
    CreatedAt = DateTime.UtcNow;
    IsActive = true;
  }

  public Guid FamilyId { get; private set; }
  public Family Family { get; private set; } = null!;
  public Guid PetId { get; private set; }
  public Pet Pet { get; private set; } = null!;
  public string Title { get; private set; } = null!;
  public int Points { get; private set; }
  public string Schedule { get; private set; } = null!;
  public TimeSpan DueDuration { get; private set; }
  public Guid CreatedBy { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public bool IsActive { get; private set; }

  public void Update(string title, int points, string schedule, TimeSpan dueDuration)
  {
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.OutOfRange(points, nameof(points), 1, 100);
    Guard.Against.NullOrWhiteSpace(schedule);
    Guard.Against.OutOfRange(dueDuration.TotalHours, nameof(dueDuration), 1, 720); // 1 hour to 30 days

    Title = title.Trim();
    Points = points;
    Schedule = schedule.Trim();
    DueDuration = dueDuration;
  }

  public void Deactivate() => IsActive = false;
}
