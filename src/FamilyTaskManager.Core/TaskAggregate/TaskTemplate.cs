namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskTemplate : EntityBase<TaskTemplate, Guid>, IAggregateRoot
{
  private TaskTemplate() { }

  public TaskTemplate(Guid familyId, Guid petId, string title, int points, string schedule, Guid createdBy)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(petId);
    Guard.Against.Default(createdBy);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.OutOfRange(points, nameof(points), 1, 100);
    Guard.Against.NullOrWhiteSpace(schedule);

    FamilyId = familyId;
    PetId = petId;
    Title = title.Trim();
    Points = points;
    Schedule = schedule.Trim();
    CreatedBy = createdBy;
    CreatedAt = DateTime.UtcNow;
    IsActive = true;
  }

  public Guid FamilyId { get; private set; }
  public Guid PetId { get; private set; }
  public string Title { get; private set; } = null!;
  public int Points { get; private set; }
  public string Schedule { get; private set; } = null!;
  public Guid CreatedBy { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public bool IsActive { get; private set; }

  public void Update(string title, int points, string schedule)
  {
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.OutOfRange(points, nameof(points), 1, 100);
    Guard.Against.NullOrWhiteSpace(schedule);

    Title = title.Trim();
    Points = points;
    Schedule = schedule.Trim();
  }

  public void Deactivate()
  {
    IsActive = false;
  }
}
