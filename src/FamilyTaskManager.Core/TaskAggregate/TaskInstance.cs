using FamilyTaskManager.Core.TaskAggregate.Events;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskInstance : EntityBase<TaskInstance, Guid>, IAggregateRoot
{
  private TaskInstance() { }

  public TaskInstance(Guid familyId, Guid petId, string title, int points, TaskType type, DateTime dueAt,
    Guid? templateId = null)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(petId);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.OutOfRange(points, nameof(points), 1, 100);

    FamilyId = familyId;
    PetId = petId;
    Title = title.Trim();
    Points = points;
    Type = type;
    TemplateId = templateId;
    Status = TaskStatus.Active;
    CreatedAt = DateTime.UtcNow;
    DueAt = dueAt;

    RegisterDomainEvent(new TaskCreatedEvent(this));
  }

  public Guid FamilyId { get; private set; }
  public Guid PetId { get; private set; }
  public string Title { get; private set; } = null!;
  public int Points { get; private set; }
  public TaskType Type { get; private set; }
  public Guid? TemplateId { get; private set; }
  public TaskStatus Status { get; private set; }
  public Guid? CompletedBy { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime DueAt { get; private set; }

  public void Start()
  {
    if (Status != TaskStatus.Active)
    {
      return;
    }

    Status = TaskStatus.InProgress;
  }

  public void Complete(Guid completedBy, DateTime completedAtUtc)
  {
    if (Status == TaskStatus.Completed)
    {
      return;
    }

    Guard.Against.Default(completedBy);

    Status = TaskStatus.Completed;
    CompletedBy = completedBy;
    CompletedAt = completedAtUtc;

    RegisterDomainEvent(new TaskCompletedEvent(this));
  }

  /// <summary>
  ///   Triggers a reminder for this task
  /// </summary>
  public void TriggerReminder()
  {
    // Only send reminders for active or in-progress tasks
    if (Status != TaskStatus.Active && Status != TaskStatus.InProgress)
    {
      return;
    }

    RegisterDomainEvent(new TaskReminderDueEvent(this));
  }
}
