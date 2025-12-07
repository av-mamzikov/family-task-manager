using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskTemplate : EntityBase<TaskTemplate, Guid>, IAggregateRoot
{
  private TaskTemplate()
  {
  }

  public TaskTemplate(Guid familyId, Guid spotId, TaskTitle title, TaskPoints points, Schedule schedule,
    DueDuration dueDuration,
    Guid createdBy)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(spotId);
    Guard.Against.Default(createdBy);
    Guard.Against.Null(title);
    Guard.Against.Null(points);
    Guard.Against.Null(schedule);
    Guard.Against.Null(dueDuration);

    FamilyId = familyId;
    SpotId = spotId;
    Title = title;
    Points = points;
    Schedule = schedule;
    DueDuration = dueDuration;
    CreatedBy = createdBy;
    CreatedAt = DateTime.UtcNow;
  }

  public Guid FamilyId { get; private set; }
  public Family Family { get; private set; } = null!;
  public Guid SpotId { get; private set; }
  public SpotBowsing SpotBowsing { get; private set; } = null!;
  public TaskTitle Title { get; private set; } = null!;
  public TaskPoints Points { get; private set; } = null!;
  public Schedule Schedule { get; private set; } = null!;
  public DueDuration DueDuration { get; private set; } = null!;
  public Guid CreatedBy { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public void Update(TaskTitle title, TaskPoints points, Schedule schedule, DueDuration dueDuration)
  {
    Guard.Against.Null(title);
    Guard.Against.Null(points);
    Guard.Against.Null(schedule);
    Guard.Against.Null(dueDuration);

    Title = title;
    Points = points;
    Schedule = schedule;
    DueDuration = dueDuration;
  }
}
